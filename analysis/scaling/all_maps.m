%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
% Copyright (c) 2024 Meta Platforms, Inc. and affiliates
% This source code is licensed under the license found in the
% LICENSE file in the root directory of this source tree.
%
% Contact:
% Zhongshi Jiang (jzs@meta.com)
% Alex Chapiro (alex@chapiro.net) 
%
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

%%%%% Main Script
function all_maps(prefix, bs, types)
% prefix = '../../data/main_study/head';
% bs = 100;
remove_outliers = 1;

D1 = readtable( [prefix, '1', '_Experiment_History', '.csv' ]);
D2 = readtable( [prefix, '2', '_Experiment_History', '.csv' ]);
D3 = readtable( [prefix, '3', '_Experiment_History', '.csv' ]);

D = [D1;D2; D3];

addpath("pwcmp")

if remove_outliers
    % outliers
    ots = readtable('outlier-mini.csv');
    out_obs = ots(logical(ots.Var3 > 1.5),:).Var1;
    
    D = D(~ismember(D.observer, out_obs),:);
end


%% Consolidate Artifact, keep level
if ismember("level-location", types)
D_lv = D;
D_lv.cond_A = strcat( ...
    num2str(D.level_A), '_', D.part_A);
D_lv.cond_B = strcat( ...
    num2str(D.level_B), '_', D.part_B);
for i = 1:5
    D_lv.cond_A = replace(D_lv.cond_A, [num2str(i),'_ref'], '0_ref');
    D_lv.cond_B = replace(D_lv.cond_B, [num2str(i),'_ref'], '0_ref');
end

D_lv.class = repmat({'head'} ,size(D,1),1);

[R, ~] = pw_scale_table( D_lv, 'class', { 'cond_A', 'cond_B' }, ...
    'observer', 'is_A_selected', 'bootstrap_samples', bs);

writetable(R, [prefix, '-final-level-location-jod.csv'])
end

%% level is implicitly consolidated. Only jpg_m0
D.cond_A = strcat( ...
    D.distortion_A, '_', (D.part_A));

D.cond_B = strcat( ...
    D.distortion_B, '_', (D.part_B));
% 

D.class = repmat({'head'} ,size(D,1),1);

if ismember("perartifact", types)
[R, ~] = pw_scale_table( D, 'class', { 'cond_A', 'cond_B' }, ...
    'observer', 'is_A_selected', 'bootstrap_samples', bs);

writetable(R, [prefix, '-final-perartifact-jod.csv'])
end

%% only location, m0, final joint map.
if ismember("final", types)

[R, ~] = pw_scale_table( D, 'class', { 'part_A', 'part_B' }, ...
    'observer', 'is_A_selected', 'bootstrap_samples', bs);

writetable(R, [prefix, '-final-joint-jod.csv'])
end
end
