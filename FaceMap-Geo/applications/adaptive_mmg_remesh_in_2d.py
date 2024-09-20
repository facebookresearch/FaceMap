#!/usr/bin/env python
# coding: utf-8

import json
from typing import List, Tuple
import scipy.interpolate
import glob
import sys
import re
sys.path.append('..')
import numpy as np
import logging
import tqdm
import igl
import visualize.vis_utils as vu
import bisect
import PIL
# import meshplot as mp
import distortion_utils as du
import os
import pandas as pd
from matplotlib import cm
import subprocess
import argparse
from PIL import ImageOps
import trimesh
import tempfile
import triangle
logging.basicConfig(level=logging.INFO)


path_prefix = ""
if os.name == 'posix':
    mmg_target = "./mmg2d_O3"
    mmg_bin_path = "/Users/jzs/Downloads/FaceMap/bin/"
else:
    path_prefix = "C:"
    mmg_target = "mmg2d_O3.exe"
    mmg_bin_path = path_prefix + "/Users/jzs/Downloads/bin/"

def remove_unreferenced(v, vi):
    vif = vi.flatten()
    uid, iid = np.unique(vif, return_inverse=True)
    return v[uid], iid.reshape(*vi.shape)

def merge_meshes(
    v_list: List[np.ndarray], f_list: List[np.ndarray], eps: float = 1e-12
) -> Tuple[np.ndarray, np.ndarray]:
    """
    Given a list of vertices and faces [(V1, F1), (V2, F2), ...], merge them into a single mesh (V,F)
    """
    offset = 0
    for i, vi in enumerate(v_list):
        f_list[i] = f_list[i] + offset
        offset += len(vi)

    if eps >= 0:
        uv_final, _, _, tf_final = igl.remove_duplicate_vertices(
            np.vstack(v_list), np.vstack(f_list), eps
        )
        return uv_final, tf_final
    else:
        return np.vstack(v_list), np.vstack(f_list)



def reading_from_mmg(file):
    with open(file, 'r') as fp:
        lines = [l for l in fp.readlines()]
    vid = next(i for i, l in enumerate(lines) if 'Verti' in l)
    tid = next(i for i, l in enumerate(lines) if 'Trian' in l)
    num_v, num_t = int(lines[vid+1]), int(lines[tid+1])
    # return lines[vid+2 : vid+2+num_v]
    verts = [list(map(float,l.split())) for l in lines[vid+2 : vid+2+num_v]]
    tris = [list(map(int,l.split()[:-1])) for l in lines[tid+2 : tid+2+num_t]]
    return np.array(verts), np.array(tris) -1


def write_to_mmg(file, v,f):
    lines = 'MeshVersionFormatted 2\n\nDimension 2\n\n'
    lines += f'Vertices\n{len(v)}\n'
    lines += ''.join([f'{i[0]} {i[1]} 0\n' for i in v])
    lines += f'Triangles\n{len(f)}\n'
    lines += ''.join([f'{i[0]} {i[1]} {i[2]} 1\n' for i in f+1])
    lines += 'End\n'
    with open(file, 'w') as fp:
        fp.writelines(lines)


def write_mmg_sol(file, val):
    lines = 'MeshVersionFormatted 2\n\nDimension 2\n\nSolAtVertices\n\n'
    lines += f'{len(val)}\n 1 1\n'
    lines += '\n'.join([str(a) for a in val])
    lines += '\nEnd'
    with open(file, 'w') as fp:
        fp.writelines(lines)
    return None


def array_of_jod(jods, ind):
    l = np.zeros(len(ind))
    l[ind] = jods
    return l, np.sort(ind)

def rectangle(rows, cols):
    m, n = len(rows), len(cols)
    v = np.array(np.meshgrid(rows,cols)).reshape(-1,m*n).T
    p = (np.arange(n-1).reshape(-1,1) * m + np.arange(m-1).reshape(1,-1)).flatten()
    f = np.concatenate([
            np.stack([p,p+m+1, p+1]).T, 
            np.stack([p, p + m, p + m + 1]).T
        ])
    return v,f


def to3d(uv2):
    uv3 = np.zeros((len(uv2),3))
    uv3[:,:2] = uv2
    return uv3


def snap_inside(tv, tf, v0, f0):
    pts, dists_, tris_ = trimesh.proximity.closest_point(trimesh.Trimesh(to3d(tv),tf), 
                                    v0)

    inside = ((dists_[f0].min(axis=1) < 5e-3)
              * 
            (igl.doublearea(pts, f0) > 1e-8))
    return pts, inside

def reverse_snap(coarse_v, coarse_f, fine_v, fine_bnd):
    bl = igl.boundary_facets(coarse_f)
    pp, dd, tt = trimesh.proximity.closest_point(trimesh.Trimesh(coarse_v, np.array([bl[:,0], bl[:,1], bl[:,1]]).T),
                                fine_v[fine_bnd]
                               )
    snapped_v = fine_v.copy()
    snapped_v[fine_bnd] = pp
    return snapped_v
    

def prepare(snapper=False, head="shead1"):
    if snapper:
        v,tv,_, f,tf,_ = igl.read_obj(f'../data/{head}.obj')
        pts = np.load('../data/remesh-snapper-landmark.npy')
    else: # facedome
        logging.warning("Warning: FaceDome should be deprecated")
        v,tv,_, f,tf,_ = igl.read_obj(f'../data/{head}.obj')
        pts = np.load('../data/landmarks.npy')


    table = pd.read_csv('../results/head-combined-jod-with-levels.csv')
    table.jod = table.jod - table[table.condition.str.contains('ref')].jod.item()
    simp_table = table[table.condition.str.contains('simp')]

    ### Get average edge length.
    ### Inside the gen.py script, I have injected prints to gather statistics for regional average edge lengths in the simplification mode for levels 1 and 2.
    ### Then for each region, we can interpolate between L1 and L2 to get some desired edge length computations.    
    avg_edge_len = np.zeros((32, 3))
    def process(x, y, w, z): 
        avg_edge_len[int(x), int(y[1])] = avg_edge_len[int(x), int(y[1])] + float(w) / 3 # divide by 3 since there are occurences for all of 3 headsvim tes
    with open(f'../applications/simp-edge-length.txt') as fp:
        ([process(*l.split()) for l in fp.readlines()]) # updated from 2D uv length to 3D length. So this is not UV-dependent.
    _, el1, el2 = avg_edge_len.T
    # print("edge length for simplification test meshes with levels 1,2:", el1, el2)

    sparse_vals = [np.zeros(32), np.zeros(32)]
    for lv in [1,2]:
        simp_table = table[table.condition.str.contains(f'{lv}_simp')]
        jod_vals,jod_keys = array_of_jod(np.asarray(simp_table['jod']), list(map(lambda x:int(x.split('_')[-1][1:]), np.asarray(simp_table['condition'])))) 
        logging.debug('Note: auto complete left side of the face')
        jod_vals,jod_keys = np.concatenate([jod_vals, jod_vals[6:]]), np.concatenate([jod_keys, np.arange(6,6+13)+13])
        sparse_vals[lv-1][jod_keys] = jod_vals
    simp1, simp2 = sparse_vals
    return simp1, simp2, el1, el2, v, f, tv, tf, pts

def full_saliency_map_interp(level, f, tv, tf, color, lift_v_f):
    c0 = scipy.io.loadmat('/Users/jzs/Downloads/GeometrySaliency/shead1.mat')['ans'].reshape(-1)
    c0 -= c0.min()
    c0 /= c0.max()
    c0 = np.exp(-2*c0)
    desired_len = c0[f].mean(axis=1)/level
    pts = igl.barycenter(tv,tf)    
    print("desired.min", desired_len.min(), "max", desired_len.max())
    return perform_2d_meshing(pts, desired_len, tv, tf, color, lift_v_f)

def perform_2d_meshing(pts, desired_len, tv, tf, color, lift_v_f):
    # Interpolate to texture domain, use 512,512.
    N = 512
    overwrite=True
    with tempfile.TemporaryDirectory() as tmpdir:
        rect_v, rect_f = rectangle(np.linspace(0,1,N),np.linspace(0,1,N))
        write_to_mmg(f'{tmpdir}/rect.mesh', rect_v, rect_f)
        landmark_pixels = np.asarray(pts*N,dtype=int)
        back_verts = np.where(abs(rect_v[:,1] - 0.5) > 0.4)[0]
        mark_verts = landmark_pixels[:,0]  * N + landmark_pixels[:,1]

        def interpolate(spl_v, spl_f, keys, vals):
            interp_vals = igl.harmonic_weights(spl_v, spl_f, 
                                            b = keys,
                                            bc = vals, 
                                            k = 2)
            return interp_vals
        val = interpolate(rect_v, rect_f.astype(np.int32), 
                        np.concatenate([back_verts, mark_verts]).astype(np.int32), np.concatenate([back_verts*0 + max(desired_len), desired_len]))
        print("val. min",   val.min())
        val = np.clip(val, desired_len.min(), desired_len.max())
        np.savez_compressed(f'{tmpdir}/val.npz', val=val)


        ### External Process for adaptive remesh, 
        # Input: val
        # Outpput: v0, f0
        write_mmg_sol(f'{tmpdir}/rect.sol', val)
        output_mesh = f'{tmpdir}/rect.o.mesh'
        if os.path.exists(output_mesh):
            os.remove(output_mesh)
        output = subprocess.run(f"{mmg_target} {tmpdir}/rect.mesh", cwd=mmg_bin_path, capture_output=True, shell=True)
        # assert len(output.stdout) > 0
        if output.returncode > 0:
            print(output)
            return None

        v0,_ = reading_from_mmg(output_mesh)
    v0 = v0.copy()
    v0[:,:2] = v0[:,[1,0]]

    return keep_inside_and_triangulate(v0, tv,tf, color, lift_v_f)

def normalize(x):
    return (x - x.min())/(x.max() - x.min())

def interpolate_and_2d_meshing(target_JND, simp1, simp2, el1, el2, tv, tf, pts, color, lift_v_f):
    assert el1.min() > 0 and el2.min() > 0
    # Old snippet to linear interpolate between.
    # desired_len_log = np.asarray([scipy.interpolate.interp1d([s1, s2], 
                                                        #  [e1, e2],fill_value="extrapolate")(target_JND) 
                                                        #  for s1,s2,e1,e2 in zip(simp1, simp2, np.log(el1), np.log(el2))])
    # desired_len = np.exp(desired_len_log) 
    abs_jnd = - (simp2 + simp1)
    desired_len = np.exp(-2 * normalize(abs_jnd)) / target_JND * 2
    print("desired.min", desired_len.min(), "max", desired_len.max())
    return perform_2d_meshing(pts, desired_len, tv, tf, color, lift_v_f)


def keep_inside_and_triangulate(v0, tv, tf, color, lift_v_f):
    snapped_pts, _, _ = trimesh.proximity.closest_point(trimesh.Trimesh(to3d(tv),tf[color==1]), 
                                    v0)
    _, dists_, _ = trimesh.proximity.closest_point(trimesh.Trimesh(to3d(tv),tf[color==0]), 
                                    snapped_pts)
    vv, ss = remove_unreferenced(tv, igl.boundary_facets(tf[color==1]))
    tri_res = triangle.triangulate({"vertices":vv.tolist() + v0[dists_ > 2e-3][:,:2].tolist(), "segments": ss.tolist()}, opts="p")

    merge_tv, merge_tf = merge_meshes([tri_res['vertices'], tv], 
                [tri_res['triangles'], tf[color==0]])

    fid, bc = du.lift_uv(tv,tf, merge_tv)
    bc = np.clip(bc,0,1)
    v,f = lift_v_f
    lifted = np.sum(v[f[fid]]*bc[:,:,None],axis=1)
    return lifted, merge_tf, merge_tv


def rect_mesh_gen(R, tv, tf, color,lift_v_f):
    sv, _ = rectangle(np.linspace(0,1,R),np.linspace(0,1,R))
    sv = sv * [1,np.sqrt(3)]
    add_v = sv + [1/2/(R-1), np.sqrt(3)/2/(R-1)]
    sv = np.vstack([sv,add_v])
    
    return keep_inside_and_triangulate(to3d(sv), tv,tf, color, lift_v_f)

def json_entry(mesh_A):
    template = {
			"dataset":"STC",
			"directory": "adamesh",
			"basemesh": "shead",
			"reference": f"{mesh_A[:6]}.obj",
			"distortionA": mesh_A,
			"distortionB":  f"{mesh_A[:6]}.obj",
			"batch":100,
			"index":0
		}
    return template

def get_fcnt(f):
    m = re.search("_f([0-9]*)_",f)
    return int(m.group(1))
def get_jcnt(f):
    m = re.search("_[JR]([0-9]*)\.",f)
    return int(m.group(1))

def bisect_solve(func, target, lower, upper):
    f_threshold = 20


    def find_closest_name(x):
        # use existing file to narrow the selections.
        d = {get_fcnt(f):f for f in glob.glob(deliver_path + f"/{head}_{algo}*")}
        k_arr = np.sort(sorted(d.keys()))
        left = bisect.bisect_left(k_arr, x)-1
        right = bisect.bisect_right(k_arr, x)
        logging.info("Narrowing %d %d", left, right)
        if left == right or right >= len(k_arr) or left == -1:
            return  - np.inf, np.inf
        return get_jcnt(d[k_arr[left]]), get_jcnt(d[k_arr[right]])
    left, right = find_closest_name(target)
    print(left, right)
    lower = max(lower, left)
    upper = min(upper, right)
    
    b0, extra0 = func(lower)
    b1, extra1 = func(upper)
    
    logging.info(f"Bisecting [{b0}, {b1}] with param{lower}-{upper}")
    if abs(b1 - b0) <= f_threshold:
        return b0, extra0
    if b0 >= target:
        return b0, extra0
    if b1 <= target:
        return b1, extra1

    mid = (lower + upper) / 2
    bm, extra = func(mid)
    if abs(bm - target) <= f_threshold:
        return bm, extra
    if bm > target:
        return bisect_solve(func, target, lower, mid)
    else:
        return bisect_solve(func, target, mid, upper)



if __name__ == "__main__":
    parser = argparse.ArgumentParser(prog="", description="")
    parser.add_argument("--debug", action="store_true")
    parser.add_argument("--algo")
    parser.add_argument("--head", default="shead1")
    parser.add_argument("--facedome",action="store_true", default=False)
    parser.add_argument("--bisect_fnum", default=-1, type=int)

    args = parser.parse_args()

    stimuli_json = []
    if args.debug:
        deliver_path = "./adamesh"
        stream_asset_path = "./"
    else:
        stream_asset_path = path_prefix + "/Users/jzs/dev/StairCaseFaceMap/Assets/StreamingAssets/"
        deliver_path =  path_prefix + "/Users/jzs/dev/StairCaseFaceMap/Assets/Resources/adamesh"

    snapper = not args.facedome
    head=args.head
    algo = args.algo
    
    simp1, simp2, el1, el2, v, f, tv, tf, pts = prepare(snapper=snapper, head=head)
    color = np.load("../data/mask_snapper.npy") if snapper else np.load("../data/mask.npy")
    out_cnt = np.count_nonzero(color==0)
    if len(color) != len(f):
        logging.warning("Color and f not match, temporarily use a sliced reference")
        color = np.zeros(len(f))
        color[:len(f) - out_cnt] = 1
    print("Backside tf:", out_cnt, "inside:", len(tf) - out_cnt, "total:", len(tf)) # outside tf: 12328 total: (44426, 3)

    lift_v_f = (v,f)

    ### Section: adaptive facemap meshes
    if algo == "ada":
        if args.bisect_fnum <= 0:
            for target_JND in tqdm.tqdm(np.arange(30, 120, 10)): # increase with worse results
                res_v, res_f, res_uv = interpolate_and_2d_meshing(target_JND, simp1, simp2, el1, el2, tv, tf, pts, color=color, lift_v_f=lift_v_f)
                filename = f"{head}_{algo}_f{len(res_f) - out_cnt:06d}_J{target_JND}.obj"
                du.write_obj(f"{deliver_path}/{filename}", res_v, res_f, res_uv, res_f)
                stimuli_json.append(json_entry(filename))
                print(f"{filename}")
                if len(res_f) < out_cnt:
                    break
            with open(stream_asset_path + f"{head}_{algo}.json", 'w') as fp:
                json.dump(dict(stimuli=stimuli_json), fp,  indent=4)
        else:
            def func(x):
                res_v, res_f, res_uv = interpolate_and_2d_meshing(x, simp1, simp2, el1, el2, tv, tf, pts, color=color, lift_v_f=lift_v_f)
                filename = f"{head}_{algo}_f{len(res_f) - out_cnt:06d}_J{x}.obj"
                du.write_obj(f"{deliver_path}/{filename}", res_v, res_f, res_uv, res_f)
                return len(res_f) - out_cnt, (res_v, res_f, res_uv)
            result_facennum, (res_v, res_f, res_uv) = bisect_solve(func, args.bisect_fnum, 1, 200)
            print("Success:", result_facennum, len(res_f))


    ### Section: spectral saliency meshes
    if algo == "spec":
        if args.bisect_fnum <= 0:
            stimuli_json = []
            
            for R in tqdm.tqdm(list(np.arange(10,100,2))):
                res_v, res_f, res_uv = full_saliency_map_interp(R, f, tv, tf, color=color, lift_v_f=lift_v_f)
                filename = f"{head}_{algo}_f{len(res_f) - out_cnt:06d}_R{R}.obj"
                du.write_obj(f"{deliver_path}/{filename}", res_v, res_f, res_uv, res_f)
                stimuli_json.append(json_entry(filename))
                if len(res_f) < out_cnt:
                    break

            with open(stream_asset_path + f"{head}_{algo}.json", 'w') as fp:
                json.dump(dict(stimuli=stimuli_json), fp,  indent=4)
        else:
            def func(x):
                res_v, res_f, res_uv =  full_saliency_map_interp(x, f, tv, tf, color=color, lift_v_f=lift_v_f)
                filename = f"{head}_{algo}_f{len(res_f) - out_cnt:06d}_R{x}.obj"
                du.write_obj(f"{deliver_path}/{filename}", res_v, res_f, res_uv, res_f)
                return len(res_f) - out_cnt, (res_v, res_f, res_uv)
            result_facennum, (res_v, res_f, res_uv) = bisect_solve(func, args.bisect_fnum, 1, 120)
            print("Success:", result_facennum, len(res_f))


    ### Section: uniform squared meshes
    if algo == "rect":
        if args.bisect_fnum <= 0:
            stimuli_json = []
            for R in tqdm.tqdm(list(range(100,200,4))):
                res_v, res_f, res_uv = rect_mesh_gen(R, tv, tf, color=color, lift_v_f=lift_v_f)
                filename = f"{head}_{algo}_f{len(res_f) - out_cnt:06d}_R{R}.obj"
                du.write_obj(f"{deliver_path}/{filename}", res_v, res_f, res_uv, res_f)
                stimuli_json.append(json_entry(filename))
                if len(res_f) < out_cnt:
                    break

            with open(stream_asset_path + f"{head}_{algo}.json", 'w') as fp:
                json.dump(dict(stimuli=stimuli_json), fp,  indent=4)
        else:
            def func(x):
                x = int(x)
                res_v, res_f, res_uv = rect_mesh_gen(int(x), tv, tf, color=color, lift_v_f=lift_v_f)
                filename = f"{head}_{algo}_f{len(res_f) - out_cnt:06d}_R{x}.obj"
                du.write_obj(f"{deliver_path}/{filename}", res_v, res_f, res_uv, res_f)
                return len(res_f) - out_cnt, (res_v, res_f, res_uv)
            result_facennum, (res_v, res_f, res_uv) = bisect_solve(func, args.bisect_fnum, 10, 200)
            print("Success:", result_facennum, len(res_f))

    if algo == "transfer":
        assert head != "shead1"
        
        files_of_head1 = glob.glob('./adamesh/shead1_ada*')
        files_of_head1 += glob.glob('./adamesh/shead1_spec*')
        files_of_head1 += glob.glob('./adamesh/shead1_rect*')
        for file in tqdm.tqdm(files_of_head1):
            _, res_uv,_,_, res_f,_ = igl.read_obj(file)

            fid, bc = du.lift_uv(tv,tf, res_uv)
            bc = np.clip(bc,0,1)

            res_v = np.sum(v[f[fid]]*bc[:,:,None],axis=1)
            filename = file.replace("shead1", head)
            du.write_obj(filename, res_v, res_f, res_uv, res_f)