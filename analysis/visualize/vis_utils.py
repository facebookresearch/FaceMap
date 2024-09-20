#########################################################
# Copyright (c) 2024 Meta Platforms, Inc. and affiliates
# This source code is licensed under the license found in the
# LICENSE file in the root directory of this source tree.
#
# Contact:
# Zhongshi Jiang (jzs@meta.com)
#
#########################################################

import meshplot as mp
import numpy as np

def sync_camera(plt, plt0):
    '''empirical snippet to assign camera of plt0 to plt'''
    plt._cam.position = plt0._cam.position
    plt._orbit.exec_three_obj_method('update')
    plt._cam.exec_three_obj_method('updateProjectionMatrix')

        
def subplot_generator(shape, shading = {}):    
    '''
    usage:
    plt = subplot_generator((1,3), dict(width=500))
    next(plt).add_mesh(v1, tf,c=block, shading=dict(flat=False))
    next(plt).add_mesh(vis, tf, shading=dict(flat=False))
    '''
    plt = None
    sh = dict(width=300,height=600, flat=False,wireframe=False)
    sh.update(shading)
    
    cnt = 0
    while True:
        vw = mp.Viewer(sh)
        if cnt == 0:
            yield vw
        plt,cnt = mp.Subplot(plt, vw, s=[shape[0],shape[1],cnt]), cnt+1
        if cnt > 1:
            yield vw
            
            
def cut_with_seam(v,f,tf):
    v1 = np.zeros((tf.max()+1, 3))
    for fi, ti in zip(f,tf):
        for j in range(3):
            v1[ti[j]] = v[fi[j]]
    return v1