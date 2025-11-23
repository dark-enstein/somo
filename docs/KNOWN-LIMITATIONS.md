# Known Limitations & Considerations

Overview of current limitations, workarounds, and future improvement areas.

---

## Current Limitations

### 1. Synthetic Training Data

**Issue**: Current model is trained on synthetic data, not real hand gestures.

**Impact**:
- Model may not generalize to real-world hand variations
- Accuracy metrics (95-98%) are optimistic upper bounds
- Real-world accuracy likely 80-90% without real training data

**Workaround**:
- Use Day 2 recorder to capture real gesture samples
- Re-train model with real data
- Expect improved robustness and lower false positives

**Timeline**: Can be done anytime after initial setup

---

### 2. Mock Hand Tracking Only

**Issue**: Currently only `MockHandTrackingProvider` is implemented.

**Impact**:
- Cannot test with real webcam or VR headset
- Gestures must be manually selected in Inspector
- No true end-to-end validation

**Workaround**:
- Mock provider sufficient for system validation
- All components tested and functional
- Real provider can be added by implementing abstract interface

**Implementation Path**:
```csharp
public class MediaPipeHandTrackingProvider : HandTrackingProvider
{
    // Integrate with MediaPipe Unity plugin or external process
    public override Vector3[] GetLandmarks() { /* ... */ }
}
```

**Timeline**: Requires MediaPipe Unity integration (1-2 days)

---

### 3. Single Hand Only

**Issue**: System currently designed for single-hand interaction.

**Impact**:
- Two-hand scaling requires both hands to grab same object
- Cannot interact with multiple objects simultaneously
- No true bimanual interactions

**Workaround**:
- Current implementation supports left OR right hand
- Two-hand scaling works within this constraint
- Multi-hand requires separate classifiers per hand

**Implementation Path**:
- Add second `GestureClassifier` for second hand
- Modify `GestureInteractionController` to handle independently
- Add logic to resolve conflicts (e.g., both hands grabbing different objects)

**Timeline**: 0.5-1 day for dual-hand support

---

### 4. Limited Gesture Set

**Issue**: Only 5 gestures currently supported.

**Impact**:
- Limited interaction vocabulary
- Some common gestures missing (swipe, peace sign, etc.)
- Cannot differentiate subtle variations

**Workaround**:
- 5 gestures sufficient for core interactions
- Extensible architecture allows adding more

**Implementation Path**:
1. Record new gesture data
2. Add to training dataset
3. Re-train model
4. Update `GestureClassifier.GESTURE_LABELS`
5. Add interaction logic

**Considerations**:
- More gestures → potential for confusion
- Keep gestures visually distinct
- Balance vocabulary size vs. accuracy

**Timeline**: 1 day per additional gesture (data + training + integration)

---

### 5. No Calibration System

**Issue**: No personal calibration for hand size variations.

**Impact**:
- Features may not align perfectly across users
- Model assumes "average" hand proportions
- Smaller/larger hands may have lower accuracy

**Workaround**:
- Scale normalization provides some invariance
- Most users should work reasonably well
- Can manually adjust confidence thresholds

**Implementation Path**:
1. Create calibration scene
2. Record user performing each gesture
3. Compute feature distribution per user
4. Apply user-specific normalization or threshold adjustments

**Timeline**: 1-2 days for basic calibration system

---

### 6. Lighting Sensitivity (MediaPipe)

**Issue**: MediaPipe hand tracking requires good lighting.

**Impact**:
- Poor lighting → tracking failure
- Backlighting causes issues
- Dark skin tones may have lower detection accuracy

**Workaround**:
- Document lighting requirements
- Use infrared tracking if available (Oculus/Ultraleap)
- Add tracking confidence monitoring

**Mitigation**:
- Check `TrackingConfidence` property
- Show warning to user if confidence < 0.7
- Gracefully degrade to "no tracking" state

**Timeline**: Already handled via `IsTracking` property

---

### 7. False Positives

**Issue**: Unintentional gestures may trigger actions.

**Impact**:
- Menu may appear during casual hand movements
- Objects grabbed accidentally
- User frustration

**Mitigation Strategies**:
- ✅ Confidence threshold (0.7 default)
- ✅ Dwell time (0.25s before gesture change)
- ✅ Majority vote smoothing
- ⬜ Gesture hold time requirement
- ⬜ Proximity gating (only recognize near interactive objects)
- ⬜ Intentionality detection (rapid gesture = intentional)

**Implementation Path**:
- Add minimum gesture hold time in `GestureClassifier`
- Add proximity checks in `GestureInteractionController`
- Tune confidence and dwell time per gesture

**Timeline**: 0.5-1 day for advanced false-positive suppression

---

### 8. No Haptic Feedback

**Issue**: No tactile confirmation of interactions.

**Impact**:
- Reduced sense of presence
- Unclear if grab succeeded
- Less natural feeling

**Workaround**:
- Visual feedback (color changes)
- Audio feedback (can be added)
- Debug UI shows state clearly

**Implementation Path**:
- Add `AudioSource` components
- Play sounds on grab/release/confirm
- If using VR controllers, add vibration

**Timeline**: 0.5 day for audio feedback

---

### 9. Occlusion Issues

**Issue**: Hand must be fully visible to camera/sensor.

**Impact**:
- Tracking lost if hand goes out of frame
- Objects block hand view
- Fingers occluded by other fingers

**Workaround**:
- `IsTracking` property detects tracking loss
- System gracefully releases grabbed objects
- User learns to keep hands visible

**Mitigation**:
- Predict hand position during brief occlusions
- Use multiple cameras/sensors
- Combine with IMU data (future)

**Timeline**: Complex, requires sensor fusion (3-5 days)

---

### 10. Performance on Low-End Hardware

**Issue**: Real-time gesture recognition has performance requirements.

**Impact**:
- May not hit 60 FPS on older machines
- VR requires higher frame rates (72-90 FPS)
- Mobile VR may be challenging

**Current Performance**:
- Desktop (M1 MacBook): 60+ FPS
- Gesture overhead: ~5ms per frame
- Plenty of headroom

**Optimization Options**:
- Use `WorkerType.CSharpBurst` (already default)
- Reduce smoothing window (5 → 3)
- Skip gesture classification every other frame
- Use simpler model (kNN with k=3)

**Timeline**: Already optimized for typical hardware

---

### 11. VR-Specific Limitations

**Issue**: Not fully tested in actual VR headset.

**Impact**:
- Unknown real-world user experience
- Comfort, fatigue, motion sickness untested
- Interaction design may need refinement

**Workaround**:
- Architecture supports VR integration
- Components are VR-ready
- Requires VR hardware for full validation

**Testing Requirements**:
- Test with Meta Quest, HTC Vive, or Valve Index
- Collect user feedback
- Iterate on interaction design
- Optimize for comfort (hand positions, gesture ergonomics)

**Timeline**: Ongoing after VR hardware available

---

## Not Implemented (Out of Scope for V1)

### Features Intentionally Omitted

1. **Continuous Gestures**: Only discrete gestures supported (no swipes, drawing)
2. **Gesture Sequences**: No gesture chaining or combos
3. **Multi-User**: Single user only (no multi-player coordination)
4. **Persistence**: No save/load of interaction state
5. **Accessibility**: No alternative input methods
6. **Localization**: English documentation only
7. **Advanced Physics**: Simple grab/throw (no complex manipulation)
8. **Networked VR**: No multiplayer networking

These are candidates for V2 (see ROADMAP.md).

---

## Workarounds Summary

| Limitation | Severity | Workaround Available? | Effort to Fix |
|------------|----------|----------------------|---------------|
| Synthetic data | Medium | ✅ Yes (record real data) | 2-4 hours |
| Mock tracking only | High | ✅ Yes (add provider) | 1-2 days |
| Single hand | Low | ✅ Yes (dual classifier) | 0.5-1 day |
| Limited gestures | Low | ✅ Yes (extensible) | 1 day per gesture |
| No calibration | Low | ⬜ Partial (threshold tuning) | 1-2 days |
| Lighting sensitivity | Medium | ⬜ Partial (IR tracking) | Hardware dependent |
| False positives | Medium | ✅ Yes (tuning) | 0.5-1 day |
| No haptics | Low | ✅ Yes (audio) | 0.5 day |
| Occlusion | Medium | ⬜ No (sensor fusion needed) | 3-5 days |
| Low-end hardware | Low | ✅ Yes (already optimized) | N/A |
| VR untested | High | ⬜ No (requires hardware) | Ongoing |

---

## Recommended Next Steps

### Short Term (1-2 weeks)

1. **Record Real Gesture Data**
   - Use `record_gestures.py` to capture 100+ samples per gesture
   - Re-train model with real data
   - Evaluate accuracy improvement

2. **Implement Real Hand Tracking Provider**
   - Choose: MediaPipe Unity plugin OR external process
   - Implement `HandTrackingProvider` interface
   - Test with live webcam

3. **Tune False-Positive Suppression**
   - Test with real users
   - Adjust confidence thresholds per gesture
   - Increase dwell time if needed

### Medium Term (1-2 months)

4. **Add More Gestures**
   - Swipe (left/right)
   - Peace sign (V)
   - OK sign
   - Record data, train, integrate

5. **Implement Calibration**
   - Build calibration scene
   - Let users record personal baseline
   - Apply per-user normalization

6. **VR Hardware Testing**
   - Test on Meta Quest with native hand tracking
   - Gather user feedback
   - Iterate on interaction design

### Long Term (3-6 months)

7. **Sensor Fusion** (handle occlusion)
8. **Multi-User Support** (networked VR)
9. **Advanced Physics** (realistic object manipulation)
10. **Accessibility Features** (alternative inputs)

See full V2 roadmap in `ROADMAP.md`.

---

## Reporting Issues

If you encounter issues not documented here:

1. Check console for error messages
2. Review day-specific guides for troubleshooting
3. Verify component configuration in Inspector
4. Test with synthetic data first, then real data
5. Document issue with:
   - Unity version
   - Platform (macOS/Windows/Linux)
   - Hand tracking provider used
   - Steps to reproduce
   - Expected vs. actual behavior

---

## Conclusion

SOMO V1 is a **functional prototype** demonstrating controller-free VR interaction via hand gestures. While limitations exist, the modular architecture enables incremental improvements. Most limitations have clear workarounds or upgrade paths.

**Production Readiness**: ⬜ Not production-ready (requires real hand tracking + user testing)

**Educational Readiness**: ✅ Excellent learning resource with complete documentation

**Prototype Readiness**: ✅ Fully functional for demonstration and iteration

---

**Last Updated**: January 2025
**Version**: 1.0
**Status**: Feature Complete (with known limitations)
