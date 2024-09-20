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

function [R, Rs] = analysisFaceMap(file, bs)
    addpath("pwcmp")
    
    D = readtable( [file ,'.csv' ]);
    D.cond_A = strcat(num2str(D.level_A),'_', ...
        D.distortion_A, '_', (D.part_A));

    D.cond_B = strcat(num2str(D.level_B),'_', ...
        D.distortion_B, '_', (D.part_B));
    for i = 1:5
        D.cond_A = replace(D.cond_A, [num2str(i),'_ref_ref'], '0_ref_ref');
        D.cond_B = replace(D.cond_B, [num2str(i),'_ref_ref'], '0_ref_ref');
    end
    [R, Rs] = pw_scale_table( D, 'scene', { 'cond_A', 'cond_B' }, ...
        'observer', 'is_A_selected', 'bootstrap_samples', bs);

    % output
    writetable(R, [file, '-jod.csv'])
end