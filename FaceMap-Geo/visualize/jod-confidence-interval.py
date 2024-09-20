#!/usr/bin/env python
# coding: utf-8

# In[1]:


from gen import *
import vis_utils as vu
import PIL
import meshplot as mp
import pandas as pd
from matplotlib import cm

from matplotlib import pyplot as plt

import plotly 


# In[18]:


table = pd.read_csv('pilot2.5jod-bs100.csv')
table = table[table['scene'] == 'head3']

ref = table.iloc[0]['jod']
table['jod'] = table['jod'] - ref
table['jod_low'] = table['jod_low'] - ref
table['jod_high'] = table['jod_high'] - ref


# In[19]:


def sanitize_str(x): return x.replace('"','').replace(' ','')

table.scene = table.scene.map(sanitize_str)
table.condition = table.condition.map(sanitize_str)


# In[20]:


from matplotlib import collections  as mc

fig, axes = plt.subplots(2,5, figsize=(60,30))
for r in [1,2]:
    for i, at in enumerate([f'{r}_jpg', f'{r}_noise', f'{r}_smooth',f'{r}_simp',f'{r}_resample']):
        ad = table[table['condition'].str.startswith(at)]
        keys = list(map(lambda x:int(x.split('_')[-1][1:]),ad.condition))
        lines = list(map(lambda x: [tuple(x[:2]), tuple(x[-2:])], np.asarray([keys, ad.jod_low,keys, ad.jod_high]).T))
        ax = axes[r-1, i]
        
        ax.scatter(keys,ad.jod, c=np.clip(keys,0,19),cmap='tab20', s=100, marker="s")
        ax.add_collection(mc.LineCollection(lines, linewidths=1))
        ax.axhline(y = table.iloc[0]['jod'], color = 'r', linestyle = '-')
        ax.axhline(y = table.iloc[0]['jod_low'], color = 'r', linestyle = '-')
        ax.axhline(y = table.iloc[0]['jod_high'], color = 'r', linestyle = '-')
        ax.set_title(at)
        ax.set_ylim(-10,10)


# ## ANOVA

# In[121]:


import sklearn
from sklearn.feature_selection import f_regression

import scipy.stats as stats


table = pd.read_csv('pilot2.5jod-bs100.csv')


dis_table = dict(ref=0, jpg=1,resample=2,smooth=3,simp=4, noise=5)
reg_table = dict(r=1,l=2,m=-1)
def func(xx):
    head = int(xx[0][-1])
    lv, dis, x = xx[1].split('_')
    if x=='ref':
        return [-1]*5
    
    lr = reg_table[x[0]]
    part = int(x[1:])
    if lr == 2:
        part = part - 13
    return [lr, head, int(lv), dis_table[dis],part]
# 
tab2 = table[['scene', 'condition']].apply(func,axis=1,result_type='expand')


f_stat, p_val = f_regression(tab2[tab2[0] > -1], table['jod'][tab2[0] > -1])


# In[122]:


print(p_val)


# In[109]:


table['jod']


# In[ ]:




