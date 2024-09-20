#!/usr/bin/env python
# coding: utf-8

# In[1]:


from gen import *
import vis_utils as vu
import PIL
import meshplot as mp
import pandas as pd
from matplotlib import cm


# In[2]:


def split_tris(v,f,fid, bc):
    pins = (v[f[fid]]*bc[:,:,None]).sum(axis=1)
    f1, cnt = [], 0
    nv = len(v)
    f = f.copy()
    for n, (ti, b) in enumerate(zip(fid,pins)):
        a0,a1,a2 = f[ti]
        f[ti] = [n + nv, a0, a1]
        f1.append([n + nv, a1, a2])
        f1.append([n + nv, a2, a0])
    return np.concatenate([v, pins]), np.concatenate([f, f1])

def region_average(jod_dict):
    for i in range(6,19):
        avg = (jod_dict[i] + jod_dict[i+13])/2
        jod_dict[i] = avg
        jod_dict[i+13] = avg

def colorbar(mn, ma, ticks = None):
    ra = ma - mn
    import matplotlib.pyplot as plt
    import numpy as np
    fig,ax = plt.subplots(figsize=(10,10))
    im = ax.imshow(np.array([[0,1]])*ra + mn, aspect=1e-4)
    ax.axis('off')
    cbar = plt.colorbar(im,orientation='horizontal', location='top')
    #cbar.set_ticks(np.linspace(1,3,5))
    #cbar.ax.set_yticklabels(np.linspace(1,3,5))
    plt.show()
colorbar(1,3)


# In[3]:


def array_of_jod(jods, ind):
    l = np.zeros(len(ind))
    l[ind] = jods
    return l, np.sort(ind)

def interpolate(spl_v, spl_f, keys, vals):
    back_verts = np.where(spl_v[:,2]<-100)[0].astype(int)
    back_vals = -np.ones(len(back_verts))

    interp_vals = igl.harmonic_weights(spl_v, spl_f, 
                                       b = np.concatenate([keys, back_verts]),
                                       bc = np.concatenate([vals, back_vals]), 
                                       k = 2)
    return interp_vals


# In[4]:


headx = 'head1'
v,tv,f,tf,bb_diag, pin2d, pins = read_template_obj_and_landmarks(head = headx)
pts = np.load('data/landmarks.npy')
fid, bc = du.lift_uv(tv,tf,pts)
colors = np.asarray(PIL.ImageOps.flip(PIL.Image.open(f'data/{headx}.png')), dtype=np.float32)[:,:,:3]/255
spl_v, spl_f = split_tris(v,f,fid,bc)


# In[5]:


table = pd.read_csv('pilot2.5_laterality_fused-jod.csv')
table = table[table['scene'] == headx]

table['jod'] = table['jod'] - table.iloc[0]['jod']
assert len(table) == 191


# In[10]:


from matplotlib import cm

plt0 = mp.plot(spl_v, spl_f, 
                  c=cm.viridis(total_map/1.5)[:,:3],  
                  shading=dict(flat=False,wireframe=False,colormap='viridis',
                              roughness=5., metalness=0.))
colorbar(0,1.5)
plt0._cam.position = (-490.17813189934077, -26.75446517847024, 340.788308840904)
plt0._orbit.exec_three_obj_method('update')
plt0._cam.exec_three_obj_method('updateProjectionMatrix')


# In[11]:


np.savez('pilot2_combined.npz', v = spl_v, f = spl_f, c = np.clip(total_map,0,1.5))


# In[7]:


# Color Plot
arti_types = ['simp', 'smooth', 'noise', 'jpg','resample']
levels = [1,2]

# plt = vu.subplot_generator((2,3))
total_map = np.zeros(len(spl_v))
sparse_vals = np.zeros(32)
for art in arti_types:
    artifact_type = f'1_{art}'
    subtable = table[table['condition'].str.startswith(artifact_type)]
    jod_vals,jod_keys = array_of_jod(np.asarray(subtable['jod']),
             list(map(lambda x:int(x.split('_')[-1]),
                     np.asarray(subtable['condition']))))
    if len(jod_keys) == 19:
        print('Note: auto complete left side of the face')
        jod_vals,jod_keys = np.concatenate([jod_vals, jod_vals[6:]]), np.concatenate([jod_keys, np.arange(6,6+13)+13])
    interp_vals = interpolate(spl_v, spl_f, keys = len(v) + jod_keys.astype(int), 
                              vals = - np.clip(jod_vals,-5,5))
    #interp_vals = np.clip(interp_vals, 0, 8)
    total_map = total_map + interp_vals
    sparse_vals = sparse_vals + jod_vals


    #tbc = igl.barycenter(tv,tf)
    #spl_tv, spl_tf = split_tris(tv,tf,fid,bc)
    #spl_tbc = igl.barycenter(spl_tv, spl_tf)
    #v1 = v.copy()

    pid = 0
    print(artifact_type)
    # mp.plot(spl_v, spl_f, 
    #               c=cm.viridis(interp_vals/3)[:,:3],  
    #               shading=dict(flat=False,wireframe=False,colormap='viridis',
    #                           roughness=5., metalness=0.))
    # colorbar(0,3)
total_map = total_map/5


# In[ ]:


artifact_type = f'1_resample'
subtable = table[table['condition'].str.startswith(artifact_type)]
jod_vals,jod_keys = array_of_jod(np.asarray(subtable['jod']),
                                list(map(lambda x:int(x.split('_')[-1][1:]),
                                     np.asarray(subtable['condition'])))
                                )
interp_vals = interpolate(spl_v, spl_f, keys = len(v) + jod_keys.astype(int), 
                          vals = - np.clip(jod_vals,-10,10))

plt0 = mp.plot(spl_v, spl_f, 
                  c=np.clip(interp_vals,0,8),  
                  shading=dict(flat=False,wireframe=False,colormap='viridis'))
colorbar(0,4)


# In[49]:


vu.sync_camera(plt0,plt)


# In[58]:


plt2 = mp.plot(du.cut_with_seam(v,f,tf), tf,
                      uv=np.vstack([tv[:,:2],[0,1]]), 
                      texture_data=colors,
                      shading=dict(flat=False),
                     )
plt2.add_points(pins[jod_keys], c = np.clip(-jod_vals,0,4), shading=dict(point_size=50.))
vu.sync_camera(plt2,plt)


# In[48]:


plt = mp.plot(du.cut_with_seam(v,f,tf), tf,
                      uv=np.vstack([tv[:,:2],[0,1]]), 
                      texture_data=colors,
                      shading=dict(flat=False),
                     )


# In[60]:


plt._cam.position


# In[81]:


x,y = pin2d[jod_keys].T

vw = mp.plot(np.vstack([x,y,1e-2*x]).T, c=np.clip(-sparse_vals/5/1.5,0,1),
             shading=dict(point_size=0.2))
vw.add_mesh(tv,tf, shading=dict(wireframe=False),uv=np.vstack([tv,[0,1]]), texture_data=colors,c= np.array([0.7,0.7,0.7]))
colorbar(0,1.5)


# In[ ]:




