import numpy as np
import igl
import scipy
import trimesh
import cv2
import triangle

def to3d(x):
    r = np.zeros((len(x),3))
    r[:,:2] = x
    return r

def combine_mesh(v_list, f_list):
    v_c = np.vstack(v_list)
    s = np.cumsum([0] + [len(l) for l in v_list])
    f_c = np.vstack([f1 + s1 for (s1,f1) in zip(s, f_list)])
    return v_c, f_c


def cut_with_seam(v,f,tf):
    v1 = np.zeros((tf.max()+1, 3))
    for fi, ti in zip(f,tf):
        for j in range(3):
            v1[ti[j]] = v[fi[j]]
    return v1

def repeat_diags(laplacian):
    return scipy.sparse.coo_matrix((np.concatenate([laplacian.data, laplacian.data, laplacian.data]),
                                    (np.concatenate([laplacian.row*3, laplacian.row*3+1, laplacian.row*3+2]),
                                     np.concatenate([laplacian.col*3, laplacian.col*3+1, laplacian.col*3+2]))))


def lift_uv(tv, tf, pts2d):
    tv3 = np.hstack([tv,np.zeros((len(tv),1))])
    if pts2d.shape[1] == 2:
        pts = np.hstack([pts2d,np.zeros((len(pts2d),1))])
    else:
        pts = pts2d
    mesh = trimesh.Trimesh(vertices=tv3, faces=tf)
    fid = mesh.ray.intersects_first(pts,
                       np.array([[0,0,-1.]]*len(pts)))
    bc = igl.barycentric_coordinates_tri(pts,
        tv3[tf[fid,0]],
        tv3[tf[fid,1]],
        tv3[tf[fid,2]]
    )
    return fid, bc


def good_setting_for_square_UV():
    # work for Proteus_eye.obj
    M, N = 9, 7
    x,y = np.meshgrid(np.arange(M)/M + 0.5/M,np.arange(N)/N + 0.5/N)
    pts = np.array([x.flatten()/1.5 + 0.15, y.flatten() / 1.5 + 0.2, x.flatten()*0.], order='F').T
    return pts

def good_setting_for_DH_UV():
    M, N = 7, 5
    x,y = np.meshgrid(np.arange(M)/M + 0.5/M,np.arange(N)/N + 0.5/N)
    pts = np.array([x.flatten()/2 + 0.25, y.flatten() / 2.5 + 0.2, x.flatten()*0.], order='F').T
    return pts


def extrinsic_perlinN(v, vn, center, radius, distortion, magnitude):
    '''
    Distortion Perlin Noise along normals
    '''
    distance= np.linalg.norm(v-center,axis=1)
    vert_in_range = np.where(distance < radius)
    v1 = v.copy()
    v1[vert_in_range] += (radius - distance[vert_in_range])[:,None]*(distortion[vert_in_range][:,None]*vn[vert_in_range])*magnitude
    return v1

def biharmonic_smoothing(v, f, center, radius):
    vert_out_range = np.linalg.norm(v-center,axis=1) >= radius
    scale = v.max()
    v = v / scale
    L = -igl.cotmatrix(v,f)
    M = igl.massmatrix(v,f)
    w = igl.harmonic_weights_from_laplacian_and_mass(L, M, np.where(vert_out_range)[0], v[vert_out_range],k=2)
    return w * scale


def screened_biharmonic_smoothing(v, f, center, radius):
    if igl.doublearea(v,f).min() > 1e-8:
        v1 = biharmonic_smoothing(v, f, center, radius)
        if np.any(np.isnan(v1)):
            print('Warning: smoothing need screen')
        return v1

    ff = igl.collapse_small_triangles(v,f, 1e-6)
    nv,nf,_,_ = igl.remove_unreferenced(v,
                                        ff)

    closest, dist, tri_id = trimesh.proximity.closest_point(trimesh.Trimesh(nv,nf), 
                                                            v)
    bcs = np.clip(igl.barycentric_coordinates_tri(closest,
                                   nv[nf[tri_id,0]],
                                   nv[nf[tri_id,1]],
                                   nv[nf[tri_id,2]]),
                  0,1)

    nv1 = biharmonic_smoothing(nv, nf, center, radius)

    v1 = np.sum(nv1[nf[tri_id]] * bcs[:,:,None], axis=1)
    if np.any(np.isnan(v1)):
        print('Warning: smoothing does not work after screen')
    return v1

def regional_smoothing(v, f, center, radius, factor=100, steps=3):
    '''factor example values: 100, 1000
    '''
    vert_out_range = np.linalg.norm(v-center,axis=1) >= radius
    for _ in range(steps):
        L = -igl.cotmatrix(v,f)
        M = igl.massmatrix(v,f)
        lmd = 1/factor # more factor, less lmd, more smooth.
        # Solve (M-delta*L) U = M*U
        P = L.T@scipy.sparse.diags(M.data**(-1))@L + lmd*M
        q = lmd*M@v
        v1 = constrained_solve(P, q, vert_out_range, v.copy())
        v = v1
    return v

def constrained_solve(A,b,flags, x): # flag ==True is known
    fixed, unknown = np.where(flags)[0], np.where(~flags)[0]
    Au = A[unknown]
    Auu, Auf = Au[:, unknown], Au[:,fixed]
    rhs = b[unknown] - Auf@x[fixed]
    xu = scipy.sparse.linalg.spsolve(Auu, rhs)
    x1 = x.copy()
    x1[unknown] = xu
    return x1


def centroid_from_mask_image(file):
    import PIL
    pngs = PIL.Image.open(file)
    colors = np.asarray(PIL.ImageOps.flip(pngs),dtype=np.float32)
    binary_map = (colors[:,:,1] < 10).astype(np.uint8)
    count, _, _, centroid = cv2.connectedComponentsWithStats(binary_map, 8, cv2.CV_32S)
    pts2d = centroid /pngs.size
    # snap middle
    middles = abs(pts2d[:,0] - 0.5) < 1e-2
    pts2d[middles,0] = 0.5

    # reflect the other side
    mirrors = np.stack([
    1 - pts2d[~middles, 0],pts2d[~middles, 1]
    ]).T
    return np.concatenate([pts2d, mirrors])


def write_obj(filename, v,f, tv, tf, mtl_name='head1.mtl'):
    with open(filename,'w') as fp:
        fp.writelines([f'mtllib {mtl_name}\n'])
        fp.writelines([f'v {p[0]} {p[1]} {p[2]}\n' for p in v])
        fp.writelines([f'vt {p[0]} {p[1]}\n' for p in tv])
        fp.writelines([f'f {i[0]}/{j[0]} {i[1]}/{j[1]} {i[2]}/{j[2]}\n' for i,j in zip(f+1,tf+1)])


def extrinsic_perlin3d(v, center, radius, distortion, magnitude):
    vert_in_range = np.where(np.linalg.norm(v-center,axis=1) < radius)
    v1 = v.copy()
    v1[vert_in_range] += np.array([distortion[d][vert_in_range] for d in range(3)]).T*magnitude
    return v1

def extrinsic_perlinN(v, vn, center, radius, distortion, magnitude = 1):
    '''extrinsic defined perlin noise along the normal direction
    '''
    distance= np.linalg.norm(v-center,axis=1)
    vert_in_range = np.where(distance < radius)
    v1 = v.copy()
    v1[vert_in_range] += (radius - distance[vert_in_range])[:,None]*(distortion[vert_in_range][:,None]*vn[vert_in_range])*magnitude
    return v1


def regional_simplify(v,uv, f,tf, radius, center, perc = 0.1):
    ''' simplification distoriton'''
    def segment_n(n):
        return np.array([np.arange(n), np.arange(1,n+1)%n]).T

    bc = igl.barycenter(v,f)
    face_out_range = np.linalg.norm(bc-center,axis=1) >= radius
    faces_in_range = np.where(~face_out_range)[0]
    bl = igl.boundary_loop(tf[faces_in_range])

    num_inter_verts = len(set(np.unique(tf[faces_in_range])) - set(bl))

    # triangulate in the UV space.
    output = triangle.triangulate(dict(vertices=uv[bl], segments=segment_n(len(bl))), opts=f'pqYYS{int(num_inter_verts*perc)}')
    simp_v, simp_f = output['vertices'], output['triangles']
    

    # combine them
    c_uv, c_tf = np.concatenate([uv,simp_v]), np.concatenate([tf[face_out_range], simp_f + len(uv)])
    coord_f, coord_bc = lift_uv(uv,tf, simp_v)
    lift_v = (v[f[coord_f]]*coord_bc[:,:,None]).sum(axis=1)
    
    print(igl.avg_edge_length(to3d(simp_v), simp_f), end=" ")
    print(igl.avg_edge_length(lift_v, simp_f))

    c_v, c_f = np.concatenate([v,lift_v]), np.concatenate([f[face_out_range], simp_f + len(v)])
    return c_v, c_uv, c_f, c_tf


def partition_mesh(v,f,center, radius):
    ''' used with old version of texture blender'''
    bc = igl.barycenter(v,f)
    face_out_range = np.linalg.norm(bc-center,axis=1) >= radius
    faces_in_range = np.where(~face_out_range)[0]
    return faces_in_range, face_out_range

def jpg_compress(colors, ratio=5):
    encode_param = [int(cv2.IMWRITE_JPEG_QUALITY), ratio]
    result, encimg = cv2.imencode('.jpg', colors*255, encode_param)
    decimg = cv2.imdecode(encimg, 1)
    # pyplot.figure(figsize = (10,10))
    # pyplot.imshow(decimg)
    return decimg/255

def blend_image(imA, imB, center, radius, p3d):
    blending = imA.copy().reshape(-1,3)
    flat_B = imB.reshape(-1,3)
    # p3d is cached data
    dists = np.linalg.norm(p3d - center,axis=1)
    blend_pix_id = np.where(dists < radius)[0]
    def wendland(d,h):
        '''offset wendland [0,f]: 1, [f,h], wendland(d-f), [h,inf], 0'''
        f = h/2
        wei = (1-(d-f)/(h-f))**4 * (4*(d-f)/(h-f) + 1)
        wei[d < f] = 1
        wei[d > h] = 0
        return wei
    weights = wendland(dists[blend_pix_id], radius)[:,None]

    blending[blend_pix_id] = (1-weights) * blending[blend_pix_id] + weights * flat_B[blend_pix_id]
    return blending.reshape(*imA.shape)

def image_resample(img, level):
    s = img.shape
    for _ in range(level):
        img = cv2.pyrDown(img)
    img = cv2.resize(img, dsize=(s[1],s[0]), interpolation=cv2.INTER_CUBIC)
    return img
