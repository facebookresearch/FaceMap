prefix = '/Users/jzs/Downloads/FaceMap-2.0-May-2024/head';
bs = 10;

D1 = readtable( [prefix, '1', '_Experiment_History', '.csv' ]);
D2 = readtable( [prefix, '2', '_Experiment_History', '.csv' ]);
D3 = readtable( [prefix, '3', '_Experiment_History', '.csv' ]);

D = [D1;D2;D3];


D.cond_A = strcat(num2str(D.level_A),'_', ...
    D.distortion_A, '_', (D.part_A));

D.cond_B = strcat(num2str(D.level_B),'_', ...
    D.distortion_B, '_', (D.part_B));

D.class = repmat({'head'} ,size(D,1),1);

[L, dist_L] = pw_scale_table_outlier(D, 'class', { 'cond_A', 'cond_B' }, ...
    'observer', 'is_A_selected', 'bootstrap_samples', bs);

u_obs = unique(D.observer);
writematrix([u_obs, L, dist_L], 'outlier-mini.csv')
