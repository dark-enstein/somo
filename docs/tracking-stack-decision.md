# Tracking Stack Decision

## Chosen Stack: MediaPipe Hands

### Rationale

After evaluating three options (Ultraleap, Oculus/Meta Hand Tracking, MediaPipe), we've chosen **MediaPipe Hands** for the following reasons:

#### Pros
1. **No Hardware Dependency**: Works with any webcam—no HMD or specialized sensor required
2. **Rapid Prototyping**: Enables ML development and testing on laptop before VR integration
3. **Cross-Platform**: Runs on Windows, macOS, Linux
4. **Well-Documented API**: Robust Python SDK with 21 hand landmarks per hand
5. **Free & Open Source**: No licensing costs or vendor lock-in
6. **Production-Ready**: Used in production apps (Google Meet, Snapchat filters)

#### Cons
1. **Not True VR**: Requires separate integration path for actual VR headset later
2. **Lower FPS**: ~30 FPS vs. 60+ FPS for native VR hand tracking
3. **Occlusion Issues**: Camera-based, so hand must be visible to sensor

### Migration Path to VR

Once gesture classifier is trained and validated:
- **Option A**: Integrate Oculus/Meta Hand Tracking SDK (same 21-landmark format)
- **Option B**: Add Ultraleap sensor to VR setup
- **Option C**: Use Quest's native hand tracking (minimal code changes—same landmark structure)

The gesture classifier model is **hardware-agnostic**—it only needs 21 3D joint positions, regardless of source.

### Technical Specifications

- **Input**: 21 landmarks × 3 coordinates (x, y, z) = 63 features per hand
- **Frame Rate**: 30 FPS (sufficient for gesture recognition)
- **Latency**: ~33ms per frame
- **Tracking Quality**: Good in well-lit environments

### Deliverables for Day 1
- ✓ Tracking stack chosen: MediaPipe
- ✓ Justification documented
- Next: Unity XR scaffolding + feature math spec
