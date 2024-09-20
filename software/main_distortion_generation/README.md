# Localized Geometric Distortion Utilities


Geometric utilities for localized distortion generation in Face Saliency Map study.
## Data
* Create an empty folder `Output/`

## Dependencies
```
conda install -c conda-forge pillow -y
pip install triangle fire trimesh rtree opencv-python libigl
```

## Run
```
python gen.py SUBCOMMAND
SUBCOMMAND is one of the following:
    [jpg
    resample
    smooth
    simp
    noise]
add -h for additional helpers for each subcommand
```

## Structure
```
- analysis/ Matlab
- 
```
## Example output


## Application: Adaptive remeshing
We work on the 2D UV domain, with mmg2d tools to generate adaptive 2d mesh.
mmg2d requires as input a **scalar field** defined on an ultra dense mesh (512x512) rect_v, rect_f. "Subsampled" vertices are then lifted up to form a mesh.

The scalar field value, as defined as "desirable edge length", would make use of the simplification driven vars, on 2 factors.
* Rescaled JND values: level1/level2 has an avg edge length for each anchor. For each anchor to have the same JND, could generate a surface defined multiplier on 3D. 
    * the result will be varying depend on target JND.
    * Now, given we want a uniform JND across all the points 
* Parameterization distortion. 3D -> 2D. Note that this is not perfectly interpolate-able.
    * Since our edge length is defined on texture domain, this step can be bypassed.
* ToDo: this should be on FaceDome, instead of snapper!!!!! 
* ToDo: what about a geometry curvature saliency

## Application: Adaptive UV mapping.