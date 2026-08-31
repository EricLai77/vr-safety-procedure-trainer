# VR Safety Procedure Trainer

A Unity/OpenXR VR training prototype where players inspect and place
emergency equipment into the correct sockets in a required sequence.

## Features

- XR grab and socket interaction
- Ordered safety procedure validation
- Correct, incorrect and duplicate placement feedback
- Magnetic socket snapping
- Hover and placement visual feedback
- Progress, timer, error count and final score
- Restartable training sessions
- JSON session result persistence
- Custom First Aid Kit asset created in Blender

## Training Flow

1. Inspect the fire extinguisher
2. Place the first aid kit
3. Place the emergency radio
4. Review the final score and restart the session

## Technology

- Unity 6.5
- C#
- OpenXR
- XR Interaction Toolkit 3.5.1
- Universal Render Pipeline
- Unity Input System
- TextMeshPro
- Blender 5.2.1

## Screenshots

![Gameplay](Documentation/Images/hero-gameplay.png)
![Socket feedback](Documentation/Images/socket-feedback.png)
![Completion screen](Documentation/Images/completion.png)

## Running the Project

1. Clone this repository.
2. Open it with Unity 6000.5.8f1.
3. Open `Assets/VRTraining/Scenes/TrainingRoom.unity`.
4. Enter Play Mode.
5. Use the XR Interaction Simulator or a compatible OpenXR headset.

## Custom 3D Asset

The First Aid Kit was modelled in Blender and exported as FBX.
The Blender source file is included under `SourceAssets/Blender`.


## Author
Runcheng Li 
