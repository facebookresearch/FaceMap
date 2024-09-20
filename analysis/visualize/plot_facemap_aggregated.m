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

function plot_facemap_aggregated()

    close all;

    labels = {'Lip Bottom', 'Lip Top', 'Nose Tip', 'Nose Bridge','Glabella','Temple',...
    'Eyebrow Start','Inner Eye','Nose Side','Mouth Corner','Eye','Eyebrow Center',...
    'Lower Jaw','Outer Eye','Lower Cheek','Upper Cheek','Eyebrow End','Upper Jaw',...
    'Jaw Joint'};
     
    jod_root = 'data/main_study/processed/bootstrap100/';
    %% heatmap figure
    if(1)

        D = readtable( [jod_root, 'head-final-perartifact-jod.csv'] );

        D2 = D;
%         D2(strcmp(D2.scene,'all'),:) = [];
%         D2(strcmp(D2.method,'reference_Level001'),:) = [];
        D2 = removevars(D2,'jod_low');
        D2 = removevars(D2,'jod_high');
        D2 = removevars(D2,'class');
        D2.jod = -1.*round(D2.jod - D2.jod(39),2);
        D2(39,:) = [];

        D_ = D2;
        sz = size(D2,1)/5;
        D2(sz+1:sz*2,:)   = D_(sz*2+1:sz*3,:);
        D2(sz*2+1:sz*3,:) = D_(sz+1:sz*2,:);

        for j = 1:size(D2,1)
            str = strsplit(D2.condition{j},'_');

            if(strcmp(str{1},'simp'))
                str{1} = 'Simplification';
            end
            if(strcmp(str{1},'resample'))
                str{1} = 'Blur';
            end
            if(strcmp(str{1},'jpg'))
                str{1} = 'Compression';
            end
            if(strcmp(str{1},'smooth'))
                str{1} = 'Smoothing';
            end
            if(strcmp(str{1},'noise'))
                str{1} = 'Noise';
            end

            D2.condition(j) = {str{1}};
            D2.location(j)  = {str{2}};
        end

        for i = 1:size(D2,1)
            string = {[D2.location{i}(1) sprintf('%02d',str2double(D2.location{i}(2:end)))]};
            D2.location(i) = string;
        end

        if(0)
            empty = '              ';
            for i = 1:length(labels)
                strang = empty;
                strang(1:size(labels{i},2)) = labels{i};
                labels{i} = strang;
            end
        end

        fig = figure('Units','normalized','Position',[0 0 .9 .3]);
        h = heatmap(D2,'location','condition','ColorVariable','jod','ColorMap',winter);
        set(gca,'FontSize',18);
        axs = struct(gca); %ignore warning that this should be avoided
        cb = axs.Colorbar;
        h.XLabel = '';
        h.YLabel = '';
        h.XDisplayLabels = labels;
        s = struct(h);
        s.XAxis.TickLabelRotation = 45;
        sorty(h,'r11','ascend')
        title(sprintf('Distortion visibility per artifact'));
        caxis([0 1.5]);
        filename = sprintf('facemap_per_artifact.pdf');
        exportgraphics(fig, filename, 'ContentType', 'vector');

    end

    %% aggregate figure

    if(0)

        D = readtable([jod_root, 'head-final-joint-jod.csv']);

        D2 = removevars(D,'class');
        ref_val = D2.jod(20);
        D2.jod      = -1.*(D2.jod      - ref_val);
%         D2.jod_low  = (D2.jod_low  - ref_val);
%         D2.jod_high = (D2.jod_high - ref_val);

        ref = [ D2.jod(20) (D2.jod_low(20)-ref_val) (D2.jod_high(20)-ref_val) ];
        D2(20,:) = [];

        for i = 1:size(D2,1)
            string = {[D2.condition{i}(1) sprintf('%02d',str2double(D2.condition{i}(2:end)))]};
            D2.condition(i) = string;
        end

        D2 = sortrows(D2,'condition');

        xval = 1:19;
        
        fig = figure('Units','normalized','Position',[0 0 .25 .375]); hold on;
        markers = {'o'};
        xlim([0.5 19.5]);
        ylim([-0.3 1.2]);
    
        yline(ref(1),'LineWidth',1,'LineStyle','-');
        yline(ref(2),'LineWidth',0.5,'LineStyle','--');
        yline(ref(3),'LineWidth',0.5,'LineStyle','--');

        h = errorbar(xval,D2.jod,D2.jod_low,D2.jod_high, 'LineStyle','none','LineWidth',1,...
            'Marker',markers{1}, 'MarkerSize', 10);
    
        for i = 1:19
            xlab{i} = D2.condition{i};
        end

%         h.XDisplayLabels = labels;

        xticks(xval);
        xticklabels(labels);
        set(gca,'XTickLabel',labels)
        set(gca,'XTick',1:19);
        %xticklabels(xlab);
        xtickangle(90);
        %xlabel('Locations');
        ylabel('Relative visibility (JOD)');
    
        legend({'Reference', 'Confidence range'},'Location','NorthEast');
        set(gca,'FontSize',12);
        grid on
    
        exportgraphics(fig, 'facemap_aggregate_plot.pdf', 'ContentType', 'vector');

    end

    %% aggregate figure TWO LEVELS

    if(1)

        D = readtable([jod_root, 'head-final-level-location-jod.csv']);

        D2 = removevars(D,'class');

        ref_loc = 1;

        ref_val = D2.jod(ref_loc);
        D2.jod      = -1.*(D2.jod      - ref_val);
        D2.jod_low  = -1.*(D2.jod_low  - ref_val);
        D2.jod_high = -1.*(D2.jod_high - ref_val);

%         ref = [ D2.jod(ref_loc) (D2.jod_low(ref_loc)-ref_val) (D2.jod_high(ref_loc)-ref_val) ];
        ref = [ D2.jod(ref_loc) (D2.jod_low(ref_loc)) (D2.jod_high(ref_loc)) ];
        D2(ref_loc,:) = [];

        D3{1} = D2(1:19  ,:);
        D3{2} = D2(20:end,:);

        for j = 1:length(D3)
            for i = 1:size(D3{j},1)
                string = {[D3{j}.condition{i}(3) sprintf('%02d',str2double(D3{j}.condition{i}(4:end)))]};
                D3{j}.condition(i) = string;
            end
            D3{j} = sortrows(D3{j},'condition');
        end

        xval = 1:19;
        
        fig = figure('Units','normalized','Position',[0 0 .25 .375]); hold on;
        markers = {'o'};
        xlim([0.5 19.5]);
        ylim([-0.3 1.5]);
    
        yline(ref(1),'LineWidth',1,'LineStyle','-');
        yline(ref(2),'LineWidth',0.5,'LineStyle','--');
        yline(ref(3),'LineWidth',0.5,'LineStyle','--','HandleVisibility','off');

        h1 = errorbar(xval-0.15,D3{1}.jod, D3{1}.jod - D3{1}.jod_low,D3{1}.jod_high - D3{1}.jod, 'LineStyle','none','LineWidth',1,...
            'Marker',markers{1}, 'MarkerSize', 10);
    
        h2 = errorbar(xval+0.15,D3{2}.jod,D3{2}.jod - D3{2}.jod_low, D3{2}.jod_high - D3{2}.jod, 'LineStyle','none','LineWidth',1,...
            'Marker',markers{1}, 'MarkerSize', 10, 'MarkerEdgeColor','r', 'Color','r');
    
        for i = 1:19
            xlab{i} = D3{1}.condition{i};
        end

%         h.XDisplayLabels = labels;

        xticks(xval);
        xticklabels(labels);
        set(gca,'XTickLabel',labels)
        set(gca,'XTick',1:19);
        %xticklabels(xlab);
        xtickangle(90);
        %xlabel('Locations');
        ylabel('Relative visibility (JOD)');
    
        legend({'Reference', 'Confidence range','Level 1','Level 2'},'Location','NorthEast');
        set(gca,'FontSize',12);
        grid on
    
        exportgraphics(fig, 'facemap_aggregate_plot.pdf', 'ContentType', 'vector');

    end


end