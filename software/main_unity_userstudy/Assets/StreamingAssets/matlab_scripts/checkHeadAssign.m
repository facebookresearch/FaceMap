function head = checkHeadAssign(headAssginFile, observer, session)
%checkHeadAssign load the assigned head for observers/sessions
%   Detailed explanation goes here
%[~,~,dataCell] = xlsread(headAssginFile);

dataCell = readtable(headAssginFile);
dataCell = [dataCell.Properties.VariableNames; table2cell(dataCell)];

head = dataCell{str2double(observer)+1, str2double(session)+1};
end

