# Face-Map Main Study Code

## Dependencies:
* Matlab (tested with R2022b, but not hard requirement)
    * Toolbox: _Statistics and Machine Learning_
    * Make sure `matlab` can be run from cmd
* Unity (tested with 2021.3.11f1)
    * Package: Universal RP

## Installation
* Open Unity at the root folder
* Key Remapping
    * left arrow
    * right arrow
    * fill in participants and session id
    * esc to quit
## Study Instructions:
* Participants 001, 002, 003
* Session 1,2,3
## Troubleshooting
* Quit in the middle of a trial and start someone else would be problematic.


# Notes:
How to debug: 
* Player Log `C:\Users\jzs\AppData\LocalLow\Meta\FaceMapStudy\Player.log`
* Editor Log

## Script Structure
* `ExperimentASAPLogic.cs`
    * `TG_2AFC_WitReference_ASAP` : `TrialGeneratorASAP`
        * `ObjectInteraction` (UI, rotation related `rotationStartAlternate`)

## Data Flow Structure
* `SessionData::GetResultFilepath`, `GetStimuliListFile`,
* `TrialGeneratorBase` Used for Trainig `TrialGeneratorASAP` for main test.
    * `Dataset::Trial`
    * `Dataset::Stimulus`
* Only writes to `JSON` file, Matlab owns all `csv`