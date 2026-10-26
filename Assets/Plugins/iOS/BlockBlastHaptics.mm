#import <Foundation/Foundation.h>
#import <CoreHaptics/CoreHaptics.h>
#import <UIKit/UIKit.h>

static CHHapticEngine* s_hapticEngine = nil;
static BOOL s_coreHapticsSupported = NO;
static BOOL s_initialized = NO;

static UIImpactFeedbackGenerator* s_lightImpact = nil;
static UIImpactFeedbackGenerator* s_mediumImpact = nil;
static UIImpactFeedbackGenerator* s_heavyImpact = nil;
static UISelectionFeedbackGenerator* s_selectionImpact = nil;
static UINotificationFeedbackGenerator* s_notificationImpact = nil;

static void EnsureEngineStarted() {
    if (s_hapticEngine != nil) {
        NSError* error = nil;
        [s_hapticEngine startAndReturnError:&error];
    }
}

extern "C" {

void _BlockBlast_Haptics_Initialize() {
    if (s_initialized) {
        return;
    }
    s_initialized = YES;

    if (@available(iOS 13.0, *)) {
        s_coreHapticsSupported = [CHHapticEngine capabilitiesForHardware].supportsHaptics;
        if (s_coreHapticsSupported) {
            NSError* error = nil;
            s_hapticEngine = [[CHHapticEngine alloc] initAndReturnError:&error];
            if (error == nil && s_hapticEngine != nil) {
                s_hapticEngine.resetHandler = ^{
                    EnsureEngineStarted();
                };
                s_hapticEngine.stoppedHandler = ^(CHHapticEngineStoppedReason reason) {
                    // Handled lazily on next play
                };
                EnsureEngineStarted();
            } else {
                s_coreHapticsSupported = NO;
                s_hapticEngine = nil;
            }
        }
    }

    if (!s_coreHapticsSupported) {
        s_lightImpact = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        [s_lightImpact prepare];
        s_mediumImpact = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
        [s_mediumImpact prepare];
        s_heavyImpact = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
        [s_heavyImpact prepare];
        s_selectionImpact = [[UISelectionFeedbackGenerator alloc] init];
        [s_selectionImpact prepare];
        s_notificationImpact = [[UINotificationFeedbackGenerator alloc] init];
        [s_notificationImpact prepare];
    }
}

int _BlockBlast_Haptics_GetCapabilities() {
    if (!s_initialized) {
        _BlockBlast_Haptics_Initialize();
    }

    if (s_coreHapticsSupported) {
        // BasicPulse (1) | Pattern (2) | Intensity (4) = 7
        return 7;
    }

    // UIKit fallback: BasicPulse (1) | Pattern (2) = 3
    return 3;
}

bool _BlockBlast_Haptics_Play(int pattern, float intensity) {
    if (!s_initialized) {
        _BlockBlast_Haptics_Initialize();
    }

    float clampedIntensity = intensity < 0.0f ? 0.0f : (intensity > 1.0f ? 1.0f : intensity);
    if (clampedIntensity <= 0.0f) {
        return false;
    }

    if (s_coreHapticsSupported && s_hapticEngine != nil) {
        if (@available(iOS 13.0, *)) {
            EnsureEngineStarted();

            NSMutableArray<CHHapticEvent*>* events = [NSMutableArray array];

            void (^addTransient)(float, float, float) = ^(float time, float intVal, float sharpness) {
                CHHapticEventParameter* pInt = [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticIntensity value:intVal];
                CHHapticEventParameter* pSharp = [[CHHapticEventParameter alloc] initWithParameterID:CHHapticEventParameterIDHapticSharpness value:sharpness];
                CHHapticEvent* evt = [[CHHapticEvent alloc] initWithEventType:CHHapticEventTypeHapticTransient parameters:@[pInt, pSharp] relativeTime:time];
                [events addObject:evt];
            };

            switch (pattern) {
                case 1: // Selection
                    addTransient(0.0f, clampedIntensity * 0.5f, 0.8f);
                    break;
                case 2: // ImpactLight
                    addTransient(0.0f, clampedIntensity * 0.6f, 0.5f);
                    break;
                case 3: // ImpactMedium
                    addTransient(0.0f, clampedIntensity * 0.8f, 0.6f);
                    break;
                case 4: // ImpactHeavy
                    addTransient(0.0f, clampedIntensity, 0.7f);
                    break;
                case 5: // Success
                    addTransient(0.0f, clampedIntensity * 0.6f, 0.4f);
                    addTransient(0.08f, clampedIntensity, 0.7f);
                    break;
                case 6: // Warning
                    addTransient(0.0f, clampedIntensity * 0.8f, 0.8f);
                    addTransient(0.10f, clampedIntensity * 0.8f, 0.8f);
                    break;
                case 7: // Error
                    addTransient(0.0f, clampedIntensity, 0.9f);
                    addTransient(0.08f, clampedIntensity * 0.8f, 0.8f);
                    addTransient(0.16f, clampedIntensity, 0.9f);
                    break;
                default:
                    return false;
            }

            NSError* error = nil;
            CHHapticPattern* patternObj = [[CHHapticPattern alloc] initWithEvents:events parameters:@[] error:&error];
            if (error == nil && patternObj != nil) {
                id<CHHapticPatternPlayer> player = [s_hapticEngine createPlayerWithPattern:patternObj error:&error];
                if (error == nil && player != nil) {
                    [player startAtTime:CHHapticTimeImmediate error:&error];
                    return error == nil;
                }
            }
            return false;
        }
    }

    // Fallback via UIKit Feedback Generators
    switch (pattern) {
        case 1: // Selection
            if (s_selectionImpact != nil) {
                [s_selectionImpact selectionChanged];
                return true;
            }
            break;
        case 2: // ImpactLight
            if (s_lightImpact != nil) {
                [s_lightImpact impactOccurred];
                return true;
            }
            break;
        case 3: // ImpactMedium
            if (s_mediumImpact != nil) {
                [s_mediumImpact impactOccurred];
                return true;
            }
            break;
        case 4: // ImpactHeavy
            if (s_heavyImpact != nil) {
                [s_heavyImpact impactOccurred];
                return true;
            }
            break;
        case 5: // Success
            if (s_notificationImpact != nil) {
                [s_notificationImpact notificationOccurred:UINotificationFeedbackTypeSuccess];
                return true;
            }
            break;
        case 6: // Warning
            if (s_notificationImpact != nil) {
                [s_notificationImpact notificationOccurred:UINotificationFeedbackTypeWarning];
                return true;
            }
            break;
        case 7: // Error
            if (s_notificationImpact != nil) {
                [s_notificationImpact notificationOccurred:UINotificationFeedbackTypeError];
                return true;
            }
            break;
    }

    return false;
}

void _BlockBlast_Haptics_Shutdown() {
    if (!s_initialized) {
        return;
    }

    if (s_hapticEngine != nil) {
        if (@available(iOS 13.0, *)) {
            [s_hapticEngine stopWithCompletionHandler:nil];
        }
        s_hapticEngine = nil;
    }

    s_lightImpact = nil;
    s_mediumImpact = nil;
    s_heavyImpact = nil;
    s_selectionImpact = nil;
    s_notificationImpact = nil;

    s_coreHapticsSupported = NO;
    s_initialized = NO;
}

}
