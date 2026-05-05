using TwentyFiveSlicer.Desktop.Models;

namespace TwentyFiveSlicer.Desktop.Services;

public sealed class SliceAssistantService
{
    public IReadOnlyList<SliceCandidateSuggestion> RecommendCandidates(
        double sourceWidth,
        double sourceHeight,
        double targetWidth,
        double targetHeight,
        TwentyFiveSliceData current)
    {
        SliceAssistantAnalysis analysis = AnalyzeDetailed(sourceWidth, sourceHeight, targetWidth, targetHeight, current);
        var candidates = new List<(string Name, TwentyFiveSliceData Data, double IntentBonus)>
        {
            ("Current slice", current, 0d),
            ("Button fit", new TwentyFiveSliceData([10d, 42d, 58d, 90d], [12d, 42d, 58d, 88d]), analysis.AssetKind == SliceAssetKind.Button ? 0.28d : 0.02d),
            ("Panel fit", new TwentyFiveSliceData([14d, 34d, 66d, 86d], [14d, 34d, 66d, 86d]), analysis.AssetKind is SliceAssetKind.Panel or SliceAssetKind.FrameHeavy ? 0.22d : 0.04d),
            ("Thin frame fit", new TwentyFiveSliceData([6d, 38d, 62d, 94d], [6d, 38d, 62d, 94d]), analysis.AssetKind == SliceAssetKind.ThinFrame ? 0.22d : 0.06d),
            ("Symmetric fit", MakeSymmetric(current), 0.12d),
            ("Centered stretch fit", new TwentyFiveSliceData([current.VerticalBorders[0], 40d, 60d, current.VerticalBorders[3]], [current.HorizontalBorders[0], 40d, 60d, current.HorizontalBorders[3]]), 0.1d)
        };

        return candidates
            .Select(candidate => ScoreCandidate(candidate.Name, candidate.Data, candidate.IntentBonus, sourceWidth, sourceHeight, targetWidth, targetHeight))
            .OrderByDescending(candidate => candidate.Score)
            .ToArray();
    }

    public IReadOnlyList<SliceCandidateSuggestion> RecommendCandidatesForTargets(
        double sourceWidth,
        double sourceHeight,
        IEnumerable<PreviewTarget> targets,
        TwentyFiveSliceData current)
    {
        PreviewTarget[] targetArray = targets.ToArray();
        if (targetArray.Length == 0)
        {
            return [];
        }

        Dictionary<string, List<SliceCandidateSuggestion>> candidatesByName = new(StringComparer.OrdinalIgnoreCase);
        foreach (PreviewTarget target in targetArray)
        {
            foreach (SliceCandidateSuggestion candidate in RecommendCandidates(sourceWidth, sourceHeight, target.Width, target.Height, current))
            {
                if (!candidatesByName.TryGetValue(candidate.Name, out List<SliceCandidateSuggestion>? candidates))
                {
                    candidates = [];
                    candidatesByName[candidate.Name] = candidates;
                }

                candidates.Add(candidate);
            }
        }

        return candidatesByName
            .Select(pair => CreateAggregateCandidate(pair.Key, pair.Value, targetArray.Length))
            .OrderByDescending(candidate => candidate.Score)
            .ToArray();
    }

    public string Analyze(double sourceWidth, double sourceHeight, double targetWidth, double targetHeight, TwentyFiveSliceData current)
    {
        SliceAssistantAnalysis analysis = AnalyzeDetailed(sourceWidth, sourceHeight, targetWidth, targetHeight, current);
        string warnings = string.Join(" ", analysis.ValidationMessages.Select(message => message.Text));
        return $"{analysis.Summary} {warnings}";
    }

    public SliceAssistantAnalysis AnalyzeDetailed(double sourceWidth, double sourceHeight, double targetWidth, double targetHeight, TwentyFiveSliceData current)
    {
        double leftBand = current.VerticalBorders[0];
        double rightBand = 100d - current.VerticalBorders[3];
        double topBand = current.HorizontalBorders[0];
        double bottomBand = 100d - current.HorizontalBorders[3];
        double protectedBandAverage = (leftBand + rightBand + topBand + bottomBand) / 4d;
        double aspect = sourceHeight <= 0d ? 1d : sourceWidth / sourceHeight;
        double symmetryError = Math.Abs(leftBand - rightBand) + Math.Abs(topBand - bottomBand);

        SliceAssetKind kind = ClassifyAsset(aspect, protectedBandAverage);
        double confidence = Math.Clamp(0.55d + (Math.Min(30d, Math.Abs(aspect - 1d) * 6d) / 100d) + ((30d - Math.Min(30d, symmetryError)) / 100d), 0.55d, 0.95d);
        string shape = FormatAssetKind(kind);

        IReadOnlyList<SliceValidationMessage> messages = SliceValidationService.Validate(
            sourceWidth,
            sourceHeight,
            targetWidth,
            targetHeight,
            current);

        var observations = new List<string>
        {
            $"Average protected edge band is {protectedBandAverage:0.#}%.",
            $"Source aspect ratio is {aspect:0.##}:1.",
            symmetryError <= 4d ? "Fixed edge bands are nearly symmetrical." : "Fixed edge bands are noticeably asymmetrical."
        };

        var actions = new List<SliceAssistantAction>
        {
            new("Center stretch", "center stretch", "Keeps the middle stretch bands predictable across output sizes."),
            new("Symmetrize edges", "symmetrize", "Makes left/right and top/bottom protected bands match.")
        };

        if (kind == SliceAssetKind.Button)
        {
            actions.Add(new SliceAssistantAction("Optimize as button", "make this a button", "Uses thinner protected edges and a wider center stretch."));
        }
        else if (kind is SliceAssetKind.Panel or SliceAssetKind.FrameHeavy)
        {
            actions.Add(new SliceAssistantAction("Optimize as panel", "make this a panel", "Protects frame edges and keeps the center flexible."));
        }

        string summary = $"This looks like a {shape} slice with {confidence:P0} confidence.";
        return new SliceAssistantAnalysis(kind, confidence, summary, observations, messages, actions);
    }

    public SliceAssistantResult Apply(string prompt, TwentyFiveSliceData current)
    {
        string normalizedPrompt = prompt.Trim().ToLowerInvariant();
        SlicePromptIntents intents = SlicePromptIntents.FromPrompt(normalizedPrompt);
        double[] vertical = TwentyFiveSliceData.NormalizeAxis(current.VerticalBorders);
        double[] horizontal = TwentyFiveSliceData.NormalizeAxis(current.HorizontalBorders);
        bool applied = false;
        var messages = new List<string>();

        if (intents.ThinnerCorners)
        {
            vertical[0] = 8d;
            vertical[3] = 92d;
            horizontal[0] = 8d;
            horizontal[3] = 92d;
            applied = true;
            messages.Add("Reduced the outer fixed corner bands.");
        }

        if (intents.ThickerCorners)
        {
            vertical[0] = Math.Max(vertical[0], 18d);
            vertical[3] = Math.Min(vertical[3], 82d);
            horizontal[0] = Math.Max(horizontal[0], 18d);
            horizontal[3] = Math.Min(horizontal[3], 82d);
            applied = true;
            messages.Add("Increased the outer fixed corner bands.");
        }

        if (intents.Symmetrize)
        {
            double horizontalBand = Math.Max(0d, ((vertical[0] + (100d - vertical[3])) / 2d));
            double verticalBand = Math.Max(0d, ((horizontal[0] + (100d - horizontal[3])) / 2d));
            vertical[0] = horizontalBand;
            vertical[3] = 100d - horizontalBand;
            horizontal[0] = verticalBand;
            horizontal[3] = 100d - verticalBand;
            applied = true;
            messages.Add("Balanced opposite fixed edge bands.");
        }

        if (intents.Panel)
        {
            vertical[0] = Math.Max(vertical[0], 14d);
            vertical[1] = 34d;
            vertical[2] = 66d;
            vertical[3] = Math.Min(vertical[3], 86d);
            horizontal[0] = Math.Max(horizontal[0], 14d);
            horizontal[1] = 34d;
            horizontal[2] = 66d;
            horizontal[3] = Math.Min(horizontal[3], 86d);
            applied = true;
            messages.Add("Optimized the slice as a panel.");
        }

        if (intents.Button)
        {
            vertical[0] = 10d;
            vertical[1] = 42d;
            vertical[2] = 58d;
            vertical[3] = 90d;
            horizontal[0] = 12d;
            horizontal[1] = 42d;
            horizontal[2] = 58d;
            horizontal[3] = 88d;
            applied = true;
            messages.Add("Optimized the slice as a button.");
        }

        if (intents.TopMatchBottom)
        {
            double bottomBand = 100d - horizontal[3];
            horizontal[0] = bottomBand;
            applied = true;
            messages.Add("Matched the top fixed band to the bottom fixed band.");
        }

        if (intents.LeftMatchRight)
        {
            double rightBand = 100d - vertical[3];
            vertical[0] = rightBand;
            applied = true;
            messages.Add("Matched the left fixed band to the right fixed band.");
        }

        if (intents.CenterStretch)
        {
            vertical[1] = 40d;
            vertical[2] = 60d;
            horizontal[1] = 40d;
            horizontal[2] = 60d;
            applied = true;
            messages.Add("Centered the main stretch regions.");
        }

        var data = new TwentyFiveSliceData(vertical, horizontal);
        string message = applied
            ? string.Join(" ", messages)
            : "I did not recognize a local command. Try thinner corners, thicker corners, symmetrize, make this a button, make this a panel, top match bottom, left match right, or center stretch.";

        return new SliceAssistantResult(data, applied, message);
    }

    private static SliceAssetKind ClassifyAsset(double aspect, double protectedBandAverage)
    {
        if (protectedBandAverage >= 25d)
        {
            return SliceAssetKind.FrameHeavy;
        }

        if (aspect >= 2.4d && protectedBandAverage <= 22d)
        {
            return SliceAssetKind.Button;
        }

        if (protectedBandAverage <= 8d)
        {
            return SliceAssetKind.ThinFrame;
        }

        if (protectedBandAverage >= 12d)
        {
            return SliceAssetKind.Panel;
        }

        return SliceAssetKind.Unknown;
    }

    private static SliceCandidateSuggestion ScoreCandidate(
        string name,
        TwentyFiveSliceData data,
        double intentBonus,
        double sourceWidth,
        double sourceHeight,
        double targetWidth,
        double targetHeight)
    {
        IReadOnlyList<SliceValidationMessage> validationMessages = SliceValidationService.Validate(
            sourceWidth,
            sourceHeight,
            targetWidth,
            targetHeight,
            data);
        int warningCount = validationMessages.Count(message => message.Severity == SliceValidationSeverity.Warning);
        double symmetryPenalty = GetSymmetryPenalty(data);
        double score = Math.Clamp(0.76d + intentBonus - (warningCount * 0.18d) - symmetryPenalty, 0d, 1d);
        var reasons = new List<string>
        {
            warningCount == 0
                ? "No validation warnings for this target."
                : $"{warningCount} validation warning(s) remain for this target.",
            symmetryPenalty <= 0.02d
                ? "Opposite fixed bands are balanced."
                : "Opposite fixed bands are uneven."
        };

        if (intentBonus > 0.15d)
        {
            reasons.Add("Asset classification strongly matches this candidate.");
        }

        return new SliceCandidateSuggestion(name, data, score, reasons);
    }

    private static SliceCandidateSuggestion CreateAggregateCandidate(string name, IReadOnlyList<SliceCandidateSuggestion> candidates, int targetCount)
    {
        double averageScore = candidates.Average(candidate => candidate.Score);
        int warningTargets = candidates.Count(candidate => candidate.Reasons.Any(reason => reason.Contains("warning", StringComparison.OrdinalIgnoreCase) && !reason.StartsWith("No ", StringComparison.OrdinalIgnoreCase)));
        var reasons = new List<string>
        {
            $"Evaluated across {targetCount} target size(s).",
            $"Average candidate score is {averageScore:P0}."
        };

        if (warningTargets == 0)
        {
            reasons.Add("No validation warnings across the tested targets.");
        }
        else
        {
            reasons.Add($"{warningTargets} target size(s) still have validation warnings.");
        }

        return new SliceCandidateSuggestion(name, candidates[0].SliceData, averageScore, reasons);
    }

    private static TwentyFiveSliceData MakeSymmetric(TwentyFiveSliceData data)
    {
        double[] vertical = TwentyFiveSliceData.NormalizeAxis(data.VerticalBorders);
        double[] horizontal = TwentyFiveSliceData.NormalizeAxis(data.HorizontalBorders);
        double horizontalBand = (vertical[0] + (100d - vertical[3])) / 2d;
        double verticalBand = (horizontal[0] + (100d - horizontal[3])) / 2d;

        vertical[0] = horizontalBand;
        vertical[3] = 100d - horizontalBand;
        horizontal[0] = verticalBand;
        horizontal[3] = 100d - verticalBand;

        return new TwentyFiveSliceData(vertical, horizontal);
    }

    private static double GetSymmetryPenalty(TwentyFiveSliceData data)
    {
        double leftBand = data.VerticalBorders[0];
        double rightBand = 100d - data.VerticalBorders[3];
        double topBand = data.HorizontalBorders[0];
        double bottomBand = 100d - data.HorizontalBorders[3];
        return Math.Min(0.18d, (Math.Abs(leftBand - rightBand) + Math.Abs(topBand - bottomBand)) / 300d);
    }

    private static string FormatAssetKind(SliceAssetKind kind)
    {
        return kind switch
        {
            SliceAssetKind.Button => "button",
            SliceAssetKind.Panel => "panel",
            SliceAssetKind.ThinFrame => "thin-frame",
            SliceAssetKind.FrameHeavy => "frame-heavy",
            _ => "general-purpose"
        };
    }

    private sealed record SlicePromptIntents(
        bool ThinnerCorners,
        bool ThickerCorners,
        bool Symmetrize,
        bool Panel,
        bool Button,
        bool TopMatchBottom,
        bool LeftMatchRight,
        bool CenterStretch)
    {
        public static SlicePromptIntents FromPrompt(string prompt)
        {
            bool mentionsCornerOrCap = HasAny(prompt, "corner", "corners", "cap", "caps", "end cap", "end caps", "edges", "ends");
            bool wantsMoreProtection = HasAny(prompt, "protect", "protected", "preserve", "keep", "stop", "avoid", "don't mess up", "do not mess up", "warped", "distorted", "deformed", "squished", "crushed", "stretched out");
            bool wantsLessProtection = HasAny(prompt, "less corner", "smaller corner", "too much border", "too chunky", "too thick", "thin out", "lighter frame");
            bool balance = HasAny(prompt, "symmetrize", "symmetrical", "symmetric", "balanced", "balance", "even", "match", "line up");
            bool sides = HasAny(prompt, "left", "right", "side", "sides");
            bool topBottom = HasAny(prompt, "top", "bottom", "vertical");
            bool center = HasAny(prompt, "center", "middle", "inner", "inside", "stretch area", "stretch band", "main area");

            return new SlicePromptIntents(
                ThinnerCorners: HasAny(prompt, "thinner corner", "thin corner") || (mentionsCornerOrCap && wantsLessProtection),
                ThickerCorners: HasAny(prompt, "thicker corner", "thick corner") || (mentionsCornerOrCap && wantsMoreProtection),
                Symmetrize: HasAny(prompt, "symmetrize", "symmetrical", "symmetric") || (balance && HasAny(prompt, "edges", "bands", "sides", "left", "right", "top", "bottom")),
                Panel: HasAny(prompt, "make this a panel", "optimize panel", "as panel", "dialog", "window frame", "frame panel", "large panel", "box background"),
                Button: HasAny(prompt, "make this a button", "optimize button", "as button", "cta", "call to action", "wide button", "pill button", "menu button", "button"),
                TopMatchBottom: HasAny(prompt, "top match bottom", "top border match") || (balance && topBottom && !sides),
                LeftMatchRight: HasAny(prompt, "left match right", "left border match") || (balance && sides),
                CenterStretch: HasAny(prompt, "center stretch", "center the stretch") || (center && HasAny(prompt, "predictable", "weird", "odd", "bad", "wrong", "too stretched", "stretchy", "stretching", "distort", "distorted", "cleaner", "stable")));
        }

        private static bool HasAny(string prompt, params string[] phrases)
        {
            return phrases.Any(phrase => prompt.Contains(phrase, StringComparison.Ordinal));
        }
    }
}
