from gen import *

import vis_utils as vu
import PIL
import meshplot as mp
import os

v,tv,_,f,tf,_ = igl.read_obj(f'C:/Users/jzs/dev/Face-Map/Assets/Resources/ExampleDataset/ASAP_Meshes/{name}.obj')


h = 3
dist = 'jpg'
plts, sbplt = [], vu.subplot_generator((4,8))
for pid in np.arange(33):
    name = f'head{h}_R{p2s(pid)}_{dist}_DL2'

    v,tv,_,f,tf,_ = igl.read_obj(f'C:/Users/jzs/dev/Face-Map/Assets/Resources/ExampleDataset/ASAP_Meshes/{name}.obj')
    
    pngfile = f'C:/Users/jzs/dev/Face-Map/Assets/Resources/ExampleDataset/Textures/{name}.png'
    if not os.path.exists(pngfile):
        pngfile = f'C:/Users/jzs/dev/Face-Map/Assets/Resources/head{h}.png'
    colors = np.asarray(PIL.ImageOps.flip(PIL.Image.open(pngfile)), dtype=np.float32)[:,:,:3]/255
    p = next(sbplt)
    p.add_mesh(du.cut_with_seam(v,f,tf)/v.max(), tf,
              uv=np.vstack([tv[:,:2],[0,1]]), 
              texture_data=colors,
              shading=dict(flat=False),
             )
       