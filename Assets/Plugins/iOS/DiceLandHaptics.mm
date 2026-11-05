#import <UIKit/UIKit.h>

// UIKit supplies device-tuned impacts, including its own envelope and hardware fallback.
// Retain four generators to avoid allocating on every clash or arriving coin.
extern "C" void DiceLand_PlayHaptic(int cue, float intensity)
{
    if ([UIApplication sharedApplication].applicationState != UIApplicationStateActive) return;
    if (@available(iOS 10.0, *))
    {
        static UIImpactFeedbackGenerator *generators[4] = { nil, nil, nil, nil };
        int index = MAX(0, MIN(3, cue));
        if (generators[index] == nil)
        {
            UIImpactFeedbackStyle style = index == 0 ? UIImpactFeedbackStyleLight
                : index == 1 ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleHeavy;
            if (@available(iOS 13.0, *))
            {
                if (index == 3) style = UIImpactFeedbackStyleRigid;
            }
            generators[index] = [[UIImpactFeedbackGenerator alloc] initWithStyle:style];
        }
        [generators[index] prepare];
        if (@available(iOS 13.0, *))
            [generators[index] impactOccurredWithIntensity:MAX(0.0f, MIN(1.0f, intensity))];
        else
            [generators[index] impactOccurred];
    }
}
