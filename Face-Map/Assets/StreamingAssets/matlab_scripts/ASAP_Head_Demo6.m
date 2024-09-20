function ASAP_Head_Demo6(observer, session)
% using PwcmpASAPScheduler and system detect the experiment file change,
% support continue experimetn for observer#_session#
% Run in the path of StreamingAssets/matlab_scripts

% set random seed
rng(str2double(observer) * 10 + str2double(session));

% load jsonlab and asap functions for matlab
addpath('jsonlab')
addpath('asap_src')

% global parameters
global task N_batch condition_table sch dataset directory trialNum continue_exp;
global stimuli_path result_path stimuliFileName trialsFileName;
task = 0;
N_batch = 250; % test 250 samples for each ob

% path and files

project_root = [pwd, '\..\..\..\'];


stimuli_path = [project_root, 'ASAPStimuliLists\'];
result_path = [stimuli_path, 'results\'];

mkdir(stimuli_path)
mkdir(result_path)

headAssign = ['head',session];

expHistoryFileName = [headAssign, '_Experiment_History.csv'];
expFileName = [observer,'_',session, '_ASAP.csv'];
stimuliFileName = ['ASAP_',observer, '_', session,'.json'];
trialsFileName = [observer, '_', session, '_', stimuliFileName];

resultsFileFullName = fullfile(result_path, trialsFileName);
historyFileFullName = fullfile(result_path, expHistoryFileName);
expFileFullName = fullfile(result_path, expFileName);
stimuliFileFullName = fullfile(stimuli_path, stimuliFileName);

fprintf("resultsFileFullName  %s\n", resultsFileFullName)
fprintf("history  Filename %s\n", historyFileFullName)
fprintf("Exp Filename %s\n", expFileFullName)
fprintf("Stimuli Filename %s\n", stimuliFileFullName)



dataset = 'Examples';
directory = 'ExampleDataset/ASAP_Meshes';

% semaphoreFile = [stimuliFileName, '_sem.bak'];
% if ~isfile(stimuliFileFullName)
%     fclose(fopen(stimuliFileFullName, 'w'));
% end
% experiment settings
% We have # scenes (images or contents), each processed by # methods at #
% different regions and each method is run using # different parameter values.

scene = {headAssign}; % scene can be a function of session
block_columns = { 'scene' };

part = { 'm0','m1','m2','m3','m4','m5','r6','r7','r8','r9','r10','r11','r12','r13','r14','r15','r16','r17','r18'};%,'l19','l20','l21','l22','l23','l24','l25','l26','l27','l28','l29','l30','l31' };
distortion = {'jpg', 'resample', 'simp', 'smooth', 'noise'};
level = [ 1, 2 ];

% new experiment or continue experiment

trialNum = 0;
continue_exp = 0;
if ~isfile(resultsFileFullName) && (~isfile(stimuliFileFullName))
    disp('[INFO] New experiment start!');
elseif isfile(resultsFileFullName)
    if isfile(expFileFullName)
        delete(expFileFullName); % delete previous exp csv file to enable repeat exp
        data = loadjson(resultsFileFullName);
        trialsdata = data.trials;
        trialNum = length(trialsdata);
        if isfile(stimuliFileFullName)
            data = loadjson(stimuliFileFullName);
            stimulidata = data.stimuli;
            stimuliNum = length(stimulidata);
        else
            stimuliNum = 0;
        end

        disp(['[INFO]', num2str(stimuliNum), ' stimulae and ', num2str(trialNum),' trails found!'])

        if trialNum == (stimuliNum-2)
            continue_exp = 1;
            disp('[INFO] Recover previous interrupted experiment!')
        elseif  trialNum == (stimuliNum-1)
            warning('[WARN] It seems all experiments have been done in this session!')
        else
            error('[Error] something wrong with the log in previous files, cannot continue!');
        end

    else
        error('[Error] something wrong with the previous files, cannot continue!');
    end
else
    error('[Error] something wrong with finding the previous files, cannot continue!');
end

task = trialNum;

% initialize ASAP, load previous experiment history if available
from_history = 1;
if ~isfile(historyFileFullName)
    fclose(fopen( historyFileFullName, 'w' ));
    from_history = 0;
elseif ~isfile(expFileFullName) % if head history exist, but experiment is new, copy to initialize ASAP.
    copyfile(historyFileFullName, expFileFullName);
    R = (readtable(expFileFullName));
    if size(R,1) == 0
	    fprintf('Populating with Header\n');
        
        fid = fopen(expFileFullName, 'w');
        fprintf(fid, ...
            "observer, scene, part_A, part_B, distortion_A, distortion_B, level_A, level_B, is_A_selected\n");
        fclose(fid);

        fid = fopen(historyFileFullName, 'w');
        fprintf(fid, ...
            "observer, scene, part_A, part_B, distortion_A, distortion_B, level_A, level_B, is_A_selected\n");
        fclose(fid);
    end
end


%cleanup = onCleanup(@()copyfile(expFileFullName, historyFileFullName));
% result listener
% file modification listener for generating experiment plan


% Create a table with all combinations of the elements from the sets
condition_table = create_factorial_table( scene, part, distortion, level );
% add 5 references to condition table

for i = 0:4
    ref=struct('scene',headAssign,'part','ref','distortion','ref','level',i);
    condition_table = [condition_table;struct2table(ref);]; %#ok<AGROW> 
end
tic
sch = PwcmpASAPScheduler(expFileFullName, observer, condition_table, block_columns );
toc
%%% The code below simulates an experiment
% [sch, N_batch] = sch.get_pair_left(); % Get the number of conditions left in the current batch
% hard coded batch number. 


% manual set all the experiments have been done for user#_session#
% this only applies to an interrupted and recovered session.
% NOTE:: that this **strongly** assume deterministic results.
% Alternatively, modify comp_M directly.
if continue_exp == 1
    for i = 1:task
        [sch, stim_A, stim_B] = sch.get_next_pair(); % Get the next pair to compare
        fprintf( 1, '>>>> Repeat compare %d with %d\n', stim_A, stim_B );
        display( condition_table([stim_A stim_B],:) );
        fprintf(1, '%s, %s\n', trialsdata(i).stimulus.distortionA, trialsdata(i).stimulus.distortionB)
        is_A_selected = (0==trialsdata(i).rating);
        fprintf( 1, 'Repeat is_A_selected with %d\n', is_A_selected );
        sch = sch.set_pair_result( is_A_selected );
    end
end


eventhandlerExpChanged(1,1);

disp('[INFO] .NET Listening !!')
expfileObj = System.IO.FileSystemWatcher(result_path);
expfileObj.Filter = expFileName;
expfileObj.EnableRaisingEvents = true;
addlistener(expfileObj,'Changed', @(src,evnt)eventhandlerExpChanged(src,evnt));

% file modification listener for collected selection result
rstfileObj = System.IO.FileSystemWatcher(result_path);
rstfileObj.Filter = trialsFileName;
rstfileObj.EnableRaisingEvents = true;
% addlistener(fileObj,'Created', @eventhandlerChanged);
addlistener(rstfileObj,'Changed', @(src,evnt)eventhandlerRstChanged(src,evnt));

while task<N_batch

    pause(0.05); %wait a reasonable short time and show debug/disp
end

copyfile(expFileFullName, historyFileFullName);
disp('Experiment finished, thanks!')

function eventhandlerExpChanged(~,~)
    % listens to the change of ExpFile.csv, controlled by ASAP.
    disp('Detect Change in File')

    disp('found stimuli generated from ASAP!')

    [sch, stim_A, stim_B] = sch.get_next_pair(); % Get the next pair to compare

    fprintf( 1, 'Compare %d with %d\n', stim_A, stim_B );
    display( condition_table([stim_A stim_B],:) );
    while strcmp(condition_table{stim_A, 'part'}{1},'ref') && strcmp(condition_table{stim_B, 'part'}{1},'ref')
        ref_comp = condition_table{stim_A, 'level'} > condition_table{stim_A, 'level'};
        sch.set_pair_result(ref_comp);
        fprintf( 1, 'Duplicate refs: set %d with %d: %s\n', stim_A, stim_B, string(ref_comp));
        [sch, stim_A, stim_B] = sch.get_next_pair();
    end
    stim_pair_to_update_json(stim_A, stim_B);

end



function dist = stim_to_filename(stim, basemesh)
    if "ref" == stim.distortion
        dist = [basemesh, '_ref.obj'];
        return
    end
    keep_right = randi([0,1]);
    lr = stim.part(1);
    if (keep_right == 1 || lr == 'm')
        dist = [basemesh, '_R', stim.part, '_', stim.distortion, '_DL', num2str(stim.level)];
        return
    else
        region = str2num(stim.part(2:end)) + 13; %#ok<ST2NM> % note: hardcoded translate to lefties.
        dist = [basemesh, '_Rl', num2str(region), '_', stim.distortion, '_DL', num2str(stim.level)];
        return
    end
    
end

function exp = stim_pair_to_update_json(stim_A, stim_B)
    pair = [table2struct(condition_table(stim_A,:)), ... 
            table2struct(condition_table(stim_B,:))];

    basemesh = pair(1).scene;
    reference = [basemesh, '_ref.obj'];
    distortionA = stim_to_filename(pair(1), basemesh);
    distortionB = stim_to_filename(pair(2), basemesh);

    exp=struct('dataset',dataset,'directory',directory,'basemesh',basemesh,...
                 'reference',reference,'distortionA',distortionA,...
                 'distortionB',distortionB,'batch', N_batch, 'index', task);
    
    jsonName = fullfile(stimuli_path, stimuliFileName);
    fprintf("History continue, JSON Name %s\n");
    if ~isfile(jsonName)
        data(1) = exp;
        data(2) = exp;
        % duplicated initial stimuli, otherwise it's not an array, the
        % first one will be not be used
        fid=fopen(jsonName,'w');
        fprintf(fid, savejson('stimuli',data));
        fclose(fid);

        fprintf( "stimuli file initilized\n" );
    else
        temp = loadjson(jsonName);
        data = temp.stimuli;
        fprintf('data index %d task %d\n',data(length(data)).index, task);
        if data(length(data)).index == task
            data(length(data)) = exp;
        else
            data(length(data) + 1) = exp;
        end

        fid=fopen(jsonName,'w');
        fprintf(fid, savejson('stimuli',data));
        fclose(fid);

        fprintf( "stimuli file updated \n" );
    end

end

function eventhandlerRstChanged(~,~)
    %     global task sch observer result_filepath;
    
        %%%%%% get selection result from unity %%%%%%%%%%
    
        % Record the result of the comparison. The result will be immediately
        % written to the file.
        pause(0.05);
        data = loadjson(fullfile(result_path, trialsFileName));
        data = data.trials;
        trialNum = length(data);
        is_A_selected = (0==data(task+1).rating);
        task = task+1;
        fprintf("%d comparison done! TrialNum = %d", task, trialNum);
    
        sch = sch.set_pair_result( is_A_selected );
        fprintf( "selection results reported to ASAP\n" );
    
end

end
