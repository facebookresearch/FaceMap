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

import json
import csv
import random
import fire
import itertools

def initial_head_assigner(filename, head_list, num_participants, num_sessions = 3):
    """ Example shufflings for the session
    observer,session 1,session 2,session 3
    1,head4,head1,head2
    """
    assert len(head_list) >= num_sessions
    header = ['observer'] + [f'session {i+1}' for i in range(num_sessions)]

    head_list = [f'head{X}' for X in head_list]

    with open(filename, 'w', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(header)
        for i in range(num_participants):
            writer.writerow([str(i+1)] + random.sample(head_list, num_sessions))


def gen_stimuli_list(filename):
    template = {
			"dataset":"Training",
			"directory":"ExampleDataset/training_meshes",
			"basemesh":" head",
			"reference":" head_ref.obj",
			"distortionA":" head_P1_D1_2",
			"distortionB":" head_P1_D1_2",
			"batch":18,
			"index":0
		}
    stimuli = dict(stimuli = [template] * 5)
    with open(filename, 'w') as fp:
        json.dump(stimuli, fp,  indent=4)

def jod(output_json, basemesh, distortion):
    # heads = ['head1', 'head2', 'head3']
    # distortions = ['jpg', 'resample', 'simp', 'smooth', 'noise']
    # basemesh = heads[0]
    # jod_test_json(basemesh, 'jpg')

    default_region = ['Rr18','Rr15', 'Rr10','Rm0','Rm2']
    regions = dict(simp=['Rr18','Rr15', 'Rr10','Rm0','Rm2'],
                   jpg=['Rr18','Rr15', 'Rr10','Rm0','Rm2'],
                   resample=['Rr18','Rr15', 'Rr10','Rm0','Rm2'],
                   smooth=['Rr18','Rr15', 'Rr10','Rm0','Rm2'],
                   noise=['Rr18','Rr15', 'Rr10','Rm0','Rm2'])

    levels = dict(jpg = [85, 87, 90, 93],
                  simp = [5,10,20,40],
                  resample=[2, 3, 4, 5],
                  smooth=[25,50,75,100],
                  noise=[0.05,0.1,0.15])

    directory = 'ExampleDataset/Meshes'
    data = []

    ref = basemesh + '_ref.obj'
    entry = dict(dataset='Example', directory=directory, basemesh=basemesh,
                 reference=ref, init_rotation_z = 0, init_scale = 1)
    for r in regions[distortion]:
        level_names =  [ref] + [f'{basemesh}_{r}_{distortion}_D{a}.obj' for a in levels[distortion]]
        for pair in itertools.combinations(level_names, 2):
            data.append(entry.copy())
            data[-1].update(distortionA = pair[0], distortionB = pair[1])
    print('Generated test database with size:', len(data))
    with open(output_json, 'w') as fp:
        json.dump(dict(stimuli = data), fp, indent=4)

def jod_adhoc(output_json, basemesh, distortion):
    regions = dict(simp=['Rr18','Rr15', 'Rr10','Rm0','Rm2'],
                   jpg=['Rr18','Rr15', 'Rr10','Rm0','Rm2'],
                   resample=['Rr18','Rr15', 'Rr10','Rm0','Rm2'],
                   smooth=['Rr18','Rr15', 'Rr10','Rm0','Rm2'],
                   noise=['Rr18','Rr15', 'Rr10','Rm0','Rm2'])

    levels = dict(jpg = [85, 87, 90, 93],
                  simp = [5,10,20,40],
                  resample=[2, 3, 4, 5],
                  smooth=[25,50,75,100],
                  noise=[0.05,0.1,0.15])

    directory = 'ExampleDataset/Meshes'
    data = []

    ref = basemesh + '_ref.obj'
    entry = dict(dataset='Example', directory=directory, basemesh=basemesh,
                 reference=ref, init_rotation_z = 0, init_scale = 1)
    for r in regions[distortion]:
        level_names =  [ref] + [f'{basemesh}_{r}_{distortion}_D{a}.obj' for a in levels[distortion]]
        for pair in itertools.combinations(level_names, 2):
            data.append(entry.copy())
            data[-1].update(distortionA = pair[0], distortionB = pair[1])
    print('Generated test database with size:', len(data))
    with open(output_json, 'w') as fp:
        json.dump(dict(stimuli = data), fp, indent=4)

if __name__ == '__main__':
    # initial_head_assigner('temp_HeadAssign.csv', ['head1', 'head2', 'head3', 'head4'], 20, 3)
    # jod_test_json("pairwise_stimuli.json","head1","simp")
    fire.Fire()
