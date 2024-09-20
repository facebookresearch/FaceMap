#########################################################
# Copyright (c) 2024 Meta Platforms, Inc. and affiliates
# This source code is licensed under the license found in the
# LICENSE file in the root directory of this source tree.
#
# Contact:
# Zhongshi Jiang (jzs@meta.com)
# Alex Chapiro (alex@chapiro.net) 
#
#########################################################

#!/usr/bin/env python

import PIL
import PIL.ImageOps
import igl
import numpy as np
import distortion_utils as du
from perlin import PerlinNoiseFactory
import random

def read_template_obj_and_landmarks(head = 'head1'):
    v,tv,_,f,tf,_ = igl.read_obj(f'data/{head}.obj')
    pts = np.load('data/landmarks_10_seed8.npy')

    bb_diag = np.linalg.norm(v.max(axis=0) - v.min(axis=0))

    fid, bc = du.lift_uv(tv,tf,pts)

    pins = (v[f[fid]]*bc[:,:,None]).sum(axis=1)
    pin2d = (tv[tf[fid]]*bc[:,:,None]).sum(axis=1)
    return v,tv,f,tf,bb_diag, pin2d, pins


def gen_pix2pts(output, head):
    pix = 2048
    # slow, several minutes
    v,tv,f,tf,bb_diag, pin2d, pins = read_template_obj_and_landmarks(head)
    pixel_centers = np.meshgrid((np.arange(pix) + 0.5)/pix, (np.arange(pix) + 0.5)/pix)
    pixel_centers = np.dstack(pixel_centers)
    fid, bc = du.lift_uv(tv,tf, pixel_centers.reshape(-1,2))
    p3d = (v[f[fid]]*bc[:,:,None]).sum(axis=1)

    np.savez(output, p3d=p3d, fid=fid,bc=bc)

def image_blend_distortion(name, img_op, level, regions, r, head):
    v,tv,f,tf,bb_diag, pin2d, pins = read_template_obj_and_landmarks(head)
    with np.load(f'data/pix2pts_{head}.npz') as npl:
        p3d, fid, bc = npl['p3d'], npl['fid'], npl['bc']
        p3d[fid==-1] = v.max(axis=0) + bb_diag

    mtl_format = (
    '''newmtl Mat
    illum 4
    Kd 0.00 0.00 0.00
    Ka 0.00 0.00 0.00
    Tf 1.00 1.00 1.00
    map_Kd {}
    Ni 1.00
    ''')

    texture = PIL.Image.open(f'data/{head}.png')
    colors = np.asarray(PIL.ImageOps.flip(texture),dtype=np.float32)[:,:,:3]/255.
    assert colors.shape[0] == 2048

    radius = bb_diag * r

    res_col = img_op(colors)

    if regions is None:
        regions = range(len(pins))

    for pid in regions:
        fname = f'{head}_R{p2s(pid)}_{name}_D{level}'
        new_im = du.blend_image(colors, res_col, center = pins[pid], radius = radius, p3d = p3d)
        du.write_obj('Output/' + fname + '.obj', v, f, tv, tf, mtl_name=f'{fname}.mtl')

        # produce a new mtl
        with open('Output/' + fname + '.mtl', 'w') as fp: fp.write(mtl_format.format(f'{fname}.png'))
        im = PIL.ImageOps.flip(PIL.Image.fromarray((new_im*255).astype(np.uint8), mode='RGB'))
        im.save('Output/' + fname + '.png')

def jpg(r = 0.05, levels = ['L1', 'L2'], regions = None,head = 'head1'):
    final_levels = [0,90,93]
    for level in levels:
        level_n = level
        if type(level) is str:
            level_n = final_levels[int(level[1])]
        image_blend_distortion('jpg', lambda im: du.jpg_compress(im, 100 - level_n), level, regions,  r, head)

def resample(r = 0.05, levels = ['L1', 'L2'], regions = None,head = 'head1'):
    final_levels = [0,4,6]
    for level in levels:
        level_n = level
        if type(level) is str:
            level_n = final_levels[int(level[1])]
        image_blend_distortion('resample', lambda im: du.image_resample(im, level_n), level, regions, r, head)

def smooth(r = 0.05, levels = ['L1', 'L2'], regions = None, head : str= 'head1'):
    final_levels = [0,80,100]
    v,tv,f,tf,bb_diag, _, pins = read_template_obj_and_landmarks(head)
    if regions is None:
        regions = range(len(pins))
    for pid in regions:
        b = du.screened_biharmonic_smoothing(v, f, pins[pid], radius=r*bb_diag)
        for level in levels:
            rate = level
            if type(level) is str:
                rate = final_levels[int(level[1])]
            v1 = v * (1 - rate/100) + b * (rate/100)
            du.write_obj(f'Output/{head}_R{p2s(pid)}_smooth_D{level}.obj', v1,f,tv,tf)

def simp(r = 0.05, levels = ['L1', 'L2'], regions = None, head = 'head1', **kwargs):
    """
    Simplify to ratio, default keep Perc% of vertices inside the given region
    """
    final_levels = [100,10,0]
    v,tv,f,tf,bb_diag, pin2d, pins = read_template_obj_and_landmarks(head)
    if regions is None:
        regions = range(len(pins))
    for pid in regions:
        for level in levels:
            ratio = level
            print("Simplification:", head, pid, level, end=' ')
            if type(ratio) is str:
                ratio = final_levels[int(level[1])]
            c_v, c_uv, c_f, c_tf = du.regional_simplify(v, tv, f, tf, center=pins[pid], radius=bb_diag*r, perc = ratio/100)
            # du.write_obj(f'Output/{head}_R{p2s(pid)}_simp_D{level}.obj', c_v, c_f, c_uv,c_tf)

def noise(r = 0.05, levels = ['L1', 'L2'], freq = 0.5, head = 'head1', regions=None):
    final_levels = [0,0.1,0.15]

    v,tv,f,tf,bb_diag, pin2d, pins = read_template_obj_and_landmarks(head)
    if regions is None:
        regions = range(len(pins))

    random.seed(0)
    PN = PerlinNoiseFactory(dimension=3, octaves=2)
    vn = igl.per_vertex_normals(v,f)

    distortion = np.asarray([PN(i[0]/freq,i[1]/freq,i[2]/freq) for i in v ])
    for level in levels:
        amp = final_levels[int(level[1])] if type(level) is str else level
        for pid in regions:
            v1 = du.extrinsic_perlinN(v, vn, pins[pid], radius=bb_diag*r, distortion=amp*distortion)
            du.write_obj(f'Output/{head}_R{p2s(pid)}_noise_D{level}.obj', v1, f, tv, tf)

def p2s(n):
    return f'sr{n}'
    """ landmark id to string (middle, right, left) information"""
    if n < 6:
        return f'm{n}'
    if n < 19:
        return f'r{n}'
    return f'l{n}'

def generate_all():
    for h in [1,2,3]:
        noise(head=f'head{h}')
        simp(head=f'head{h}')
        jpg(head=f'head{h}')
        resample(head=f'head{h}')
        smooth(head=f'head{h}')

if __name__ == '__main__':
    '''Instructions:
    Required data:
        [data/head[123].obj
        data/landmarks.npy
        data/head[123].png
        data/pix2pts.npz]
    python gen.py SUBCOMMAND
    SUBCOMMAND is one of the following:
        [jpg
        resample
        smooth
        simplify
        noise]

    Complete examples:
    ```
    for l in 0.2 0.5; do python gen.py noise --amp=$l &; done
    for l in 5 10 20; do python gen.py simplify --ratio=$l &; done
    for l in 1 2 3 4; do python gen.py smooth --ratio=$l&; done
    for l in 10 15 20 25; do python gen.py jpg --level=$l&; done # reverse
    for l in 2 3 4 5; do python gen.py resample  --level=$l&; done
    ```
    Or, PowerShell
    ```
    foreach($i in 40,60,80){python .\gen.py simplify --ratio $i}
    ```

    '''
    import fire
    fire.Fire()
