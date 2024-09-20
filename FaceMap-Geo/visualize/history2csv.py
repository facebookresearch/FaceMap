import csv
import pandas
import fire

def func(row):
    pa, pb = row[' part_A'][2:-1], row[' part_B'][2:-1]
    setting = 'R' + pa #(pb if pa == 'ref' else pa)
    cond_1, cond_2 = f'DL{int(row[" level_A"])}', f'DL{int(row[" level_B"])}'
    if pa == 'ref':
        cond_1 = 'ref'
        setting = 'R' + pb
    if pb == 'ref':
        cond_2 = 'ref'
    return row.observer, setting, cond_1, cond_2, 1 - row[' is_A_selected']

def main(csv_input, output):
    demo_trials_entry = {'observer':1,
     'basemesh':'head1_ref.obj',
     'dst_type':'ASAP_Meshes',
     'setting':'Rr**',
     'condition_1':'DL**',
     'condition_1_filepath': 'obj',
     'condition_2': 'DL**',
     'condition_2_filepath':'obj',
     'selection':0,
     'time':1.0}
    exp_history = pandas.read_csv(csv_input)

    pd = pandas.DataFrame([demo_trials_entry]*len(exp_history))

    pd[['observer', 'setting','condition_1', 'condition_2', 'selection']]  = exp_history.apply(func, axis=1, result_type='expand')

    pd.to_csv(output, index=False)
    
if __name__ == '__main__':
    fire.Fire(main)