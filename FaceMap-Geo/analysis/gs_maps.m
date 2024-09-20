% adapt facemap code for 3d gaussian splatting
prefix = '/Users/jzs/Downloads/FaceMap/facemap-results/head';
bs = 1;

D_load = readtable('/Users/jzs/Downloads/FaceMap/G_study_final.csv');
addpath("pwcmp")

% outliers
% ots = readtable('outlier.csv');
% out_obs = ots(logical(ots.Var3 > 1.5),:).Var1;
% D = D(~ismember(D.observer, out_obs),:);

D = (D_load);
D.class = repmat({'head'} ,size(D,1),1);

n = size(D.strategy_A);
D.strategy_A = repmat('A',n) + string(D.strategy_A) +'_' + D.subject;
D.strategy_B = repmat('B',n) + string(D.strategy_B) +'_' + D.subject;
D.score = 2 - D.preference;

[R, ~] = pw_scale_table( D, 'class', { 'strategy_A', 'strategy_B' }, ...
    'participant', 'score', 'bootstrap_samples', bs);