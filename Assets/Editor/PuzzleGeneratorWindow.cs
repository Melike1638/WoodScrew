using UnityEditor;
using UnityEngine;

public class PuzzleGeneratorWindow : EditorWindow
{
    private int levelNumber = 1;

    private PuzzleDefinition currentPuzzle;
    private ScrewPuzzleSolver.Result solveResult;
    private PuzzleDifficultyProfile.Profile currentProfile;

    private int candidatesTested;
    private float currentScore;
    private bool profileMatched;
    private double generationSeconds;
    private int matchingCandidatesFoundForDisplay;

    // Gerçek uçtan uca süre:
    // butona basıldığı andan sonuç panelinin Repaint edilmesine kadar.
    private double wallClockStartTime;
    private bool wallClockTimingPending;

    // Maksimum tarama sayısı. Generator önce 100 adayı
    // mutlaka inceler; yeterli sayıda profil-uygun aday
    // bulursa 500'ü beklemeden durur.
    private int candidateCount = 100;

    private const int MinimumCandidatesToScan = 100;
    private const int MatchingCandidatesNeeded = 12;

    [MenuItem("Tools/Puzzle Generator")]
    public static void ShowWindow()
    {
        GetWindow<PuzzleGeneratorWindow>(
            "Puzzle Üretici"
        );
    }

    void OnGUI()
    {
        GUILayout.Space(10);

        GUILayout.Label(
            "WOOD SCREW PUZZLE ÜRETİCİ",
            EditorStyles.boldLabel
        );

        GUILayout.Space(10);

        levelNumber =
            EditorGUILayout.IntSlider(
                "Bölüm (Level)",
                levelNumber,
                1,
                15
            );

        candidateCount =
            EditorGUILayout.IntSlider(
                "Aday Sayısı",
                candidateCount,
                100,
                500
            );

        if (GUILayout.Button(
            "EN İYİ ADAY PUZZLE'I ÜRET",
            GUILayout.Height(38)))
        {
            wallClockStartTime =
                EditorApplication.timeSinceStartup;

            wallClockTimingPending =
                true;

            GenerateBestCandidate();
        }

        GUILayout.Space(5);

        if (GUILayout.Button(
            "MEVCUT PUZZLE'I ÇÖZ (AKTİF SAHNE)",
            GUILayout.Height(32)))
        {
            wallClockStartTime =
                EditorApplication.timeSinceStartup;

            wallClockTimingPending =
                true;

            SolveCurrent();
        }

        if (currentPuzzle == null)
            return;

        GUILayout.Space(10);

        DrawProfile();

        GUILayout.Space(8);

        DrawResult();

        GUILayout.Space(15);

        Rect previewRect =
            GUILayoutUtility.GetRect(
                360,
                430
            );

        DrawPreview(
            previewRect
        );
    }

    private void GenerateBestCandidate()
    {
        currentProfile =
            PuzzleDifficultyProfile.Get(
                levelNumber
            );

        PuzzleDefinition bestPuzzle =
            null;

        ScrewPuzzleSolver.Result bestResult =
            null;

        float bestScore =
            float.MaxValue;

        candidatesTested = 0;
        profileMatched = false;

        int matchingCandidatesFound = 0;

        int baseSeed =
            levelNumber * 10000 +
            System.Environment.TickCount;

        try
        {
            for (int i = 0;
                 i <
                 candidateCount;
                 i++)
            {
                EditorUtility.DisplayProgressBar(
                    "Puzzle üretiliyor",
                    "Aday " +
                    (i + 1) +
                    " / " +
                    candidateCount +
                    " test ediliyor...",
                    (float)i /
                    candidateCount
                );

                PuzzleDefinition candidate =
                    PuzzleCandidateGenerator
                        .Generate(
                            levelNumber,
                            baseSeed +
                            i * 7919
                        );

                ScrewPuzzleSolver.Result result =
                    ScrewPuzzleSolver.Solve(
                        candidate,
                        currentProfile
                            .SolverStateLimit
                    );

                candidatesTested++;

                float score =
                    PuzzleDifficultyProfile
                        .Score(
                            currentProfile,
                            result
                        );

                bool candidateMatches =
                    PuzzleDifficultyProfile
                        .Matches(
                            currentProfile,
                            result
                        );

                if (candidateMatches)
                {
                    matchingCandidatesFound++;
                }

                // "En İyi Aday" artık ilk uygun adayda
                // durmuyor. Seçilen 100-500 adayın tamamı
                // taranıyor. En az bir profil-uygun aday
                // bulunduysa uygun olmayan aday bir daha
                // onun önüne geçemiyor.
                if (candidateMatches)
                {
                    if (!profileMatched ||
                        score < bestScore)
                    {
                        bestScore =
                            score;

                        bestPuzzle =
                            candidate;

                        bestResult =
                            result;
                    }

                    profileMatched = true;
                }
                else if (!profileMatched &&
                         score < bestScore)
                {
                    bestScore =
                        score;

                    bestPuzzle =
                        candidate;

                    bestResult =
                        result;
                }

                // Hız optimizasyonu:
                // İlk 100 aday her durumda taranır.
                // 100'den sonra en az 12 profil-uygun aday
                // görmüşsek artık yeterli karşılaştırma
                // havuzu vardır ve erken dururuz.
                //
                // Profil nadirse ve 12 uygun aday bulunmazsa
                // seçilen maksimum sayıya (örn. 500) kadar
                // aramaya devam eder.
                if (candidatesTested >=
                        MinimumCandidatesToScan &&
                    matchingCandidatesFound >=
                        MatchingCandidatesNeeded)
                {
                    break;
                }
            }
        }
        finally
        {
            EditorUtility
                .ClearProgressBar();
        }

        matchingCandidatesFoundForDisplay =
            matchingCandidatesFound;

        currentPuzzle =
            bestPuzzle;

        solveResult =
            bestResult;

        currentScore =
            bestScore;

        if (currentPuzzle != null &&
            solveResult != null)
        {
            profileMatched =
                PuzzleDifficultyProfile
                    .Matches(
                        currentProfile,
                        solveResult
                    );
        }
    }

    private void SolveCurrent()
    {
        PuzzleDefinition scenePuzzle =
            ScenePuzzleExtractor
                .ExtractActiveScene();

        if (scenePuzzle == null)
        {
            Debug.LogError(
                "Aktif sahne PuzzleDefinition'a çevrilemedi."
            );

            wallClockTimingPending =
                false;

            return;
        }

        currentPuzzle =
            scenePuzzle;

        currentProfile =
            PuzzleDifficultyProfile.Get(
                levelNumber
            );

        solveResult =
            ScrewPuzzleSolver.Solve(
                currentPuzzle,
                currentProfile
                    .SolverStateLimit
            );

        candidatesTested =
            1;

        currentScore =
            PuzzleDifficultyProfile.Score(
                currentProfile,
                solveResult
            );

        profileMatched =
            PuzzleDifficultyProfile.Matches(
                currentProfile,
                solveResult
            );

        matchingCandidatesFoundForDisplay =
            profileMatched
                ? 1
                : 0;
    }


    private void DrawProfile()
    {
        string text =
            "ZORLUK PROFİLİ\n\n" +

            "Bölüm: " +
            currentProfile.LevelNumber +

            "\nTahta: " +
            currentProfile.PlankCount +

            "\nBoş Delik: " +
            currentProfile.FreeHoleCount +

            "\nMinimum Hamle Hedefi: " +
            currentProfile.MinimumMovesMin +
            "-" +
            currentProfile.MinimumMovesMax +

            "\nBaşlangıç Vida Seçeneği Hedefi: " +
            currentProfile.InitialScrewChoicesMin +
            "-" +
            currentProfile.InitialScrewChoicesMax +

            "\nFizikçe Güvenli İlk Vida Hedefi: " +
            currentProfile.PhysicsSafeFirstScrewsMin +
            "-" +
            currentProfile.PhysicsSafeFirstScrewsMax +

            "\nÇarpışan İlk Vida Hedefi: " +
            (currentProfile.BlockedFirstScrewsMin >= 0 &&
             currentProfile.BlockedFirstScrewsMax >= 0
                ? currentProfile.BlockedFirstScrewsMin +
                  "-" +
                  currentProfile.BlockedFirstScrewsMax
                : "Yok") +

            "\nMaksimum Aday Tarama: " +
            candidateCount +
            "\nMinimum Tarama: " +
            MinimumCandidatesToScan +
            "\nErken Durma: " +
            MatchingCandidatesNeeded +
            " uygun aday" +

            "\nKarar Yoğunluğu Hedefi: %" +
            (currentProfile.DecisionDensityMin *
             100f).ToString("0") +
            "-%" +
            (currentProfile.DecisionDensityMax *
             100f).ToString("0") +

            "\nMaksimum Çıkmaz Oranı: %" +
            (currentProfile.DeadEndRateMax *
             100f).ToString("0");

        EditorGUILayout.HelpBox(
            text,
            MessageType.Info
        );
    }

    private void DrawResult()
    {
        if (solveResult == null)
            return;

        // Layout/MouseUp aşamasında değil, sonuç gerçekten
        // ekrana çizilirken süreyi kapatıyoruz.
        if (wallClockTimingPending &&
            Event.current.type ==
                EventType.Repaint)
        {
            generationSeconds =
                EditorApplication.timeSinceStartup -
                wallClockStartTime;

            wallClockTimingPending =
                false;
        }

        float decisionDensity =
            PuzzleDifficultyProfile
                .GetDecisionDensity(
                    solveResult
                );

        float deadEndRate =
            PuzzleDifficultyProfile
                .GetDeadEndRate(
                    solveResult
                );

        string text =
            "ADAY SONUCU\n\n" +

            "Profil Uygunluğu: " +
            (profileMatched
                ? "EVET"
                : "HAYIR") +

            "\nKalite Puanı (düşük daha iyi): " +
            currentScore
                .ToString("0.0") +

            "\nTest Edilen Aday: " +
            candidatesTested +

            "\nProfil Uygun Aday: " +
            matchingCandidatesFoundForDisplay +

            "\nToplam Gerçek Süre: " +
            generationSeconds
                .ToString("0.00") +
            " sn" +

            "\n\nÇözülebilir: " +
            (solveResult.Solvable
                ? "EVET"
                : "HAYIR") +

            "\nMinimum Hamle: " +
            solveResult.MinimumMoves +

            "\nBaşlangıç Vida Seçeneği: " +
            solveResult.InitialScrewChoices +

            "\nBaşlangıç Tahta Seçeneği: " +
            solveResult.InitialPlankChoices +

            "\nFizikçe Güvenli İlk Vida: " +
            solveResult
                .PhysicsSafeFirstScrewCount +

            "\nÇarpışan İlk Vida: " +
            solveResult
                .BlockedFirstScrewCount +

            "\nKarar Yoğunluğu: %" +
            (decisionDensity * 100f)
                .ToString("0.0") +

            "\nÇıkmaz Oranı: %" +
            (deadEndRate * 100f)
                .ToString("0.0") +

            "\nİncelenen Durum: " +
            solveResult.ExploredStates +

            "\nDurum Sınırına Ulaşıldı: " +
            (solveResult.StateLimitReached
                ? "EVET"
                : "HAYIR") +

            "\n\nBilgi amaçlı:" +

            "\nEn İyi İlk Vida Sayısı: " +
            solveResult
                .OptimalFirstScrewCount +

            "\nHam En Kısa Çözüm Kombinasyonu: " +
            solveResult
                .ShortestSolutionCount;

        EditorGUILayout.HelpBox(
            text,
            profileMatched
                ? MessageType.Info
                : MessageType.Warning
        );
    }

    private void DrawPreview(
        Rect area)
    {
        EditorGUI.DrawRect(
            area,
            new Color(
                0.55f,
                0.32f,
                0.15f
            )
        );

        Rect board =
            new Rect(
                area.x + 15,
                area.y + 15,
                area.width - 30,
                area.height - 30
            );

        EditorGUI.DrawRect(
            board,
            new Color(
                0.78f,
                0.58f,
                0.32f
            )
        );

        foreach (PlankDefinition plank
                 in currentPuzzle.Planks)
        {
            DrawPlank(
                board,
                plank
            );
        }

        PuzzleSwingPreview.Draw(
            board,
            currentPuzzle
        );

        foreach (HoleDefinition hole
                 in currentPuzzle.Holes)
        {
            DrawHole(
                board,
                hole
            );
        }

        foreach (ScrewDefinition screw
                 in currentPuzzle.Screws)
        {
            DrawScrew(
                board,
                screw
            );
        }
    }

    private void DrawPlank(
        Rect board,
        PlankDefinition plank)
    {
        Vector2 center =
            WorldToPreview(
                board,
                plank.Position
            );

        float width =
            plank.Size.x *
            55f;

        float height =
            plank.Size.y *
            55f;

        Vector2 right =
            Quaternion.Euler(
                0f,
                0f,
                -plank.Rotation
            ) *
            Vector2.right;

        Vector2 up =
            Quaternion.Euler(
                0f,
                0f,
                -plank.Rotation
            ) *
            Vector2.up;

        Vector2 halfRight =
            right *
            width *
            0.5f;

        Vector2 halfUp =
            up *
            height *
            0.5f;

        Vector3[] points =
        {
            center - halfRight - halfUp,
            center + halfRight - halfUp,
            center + halfRight + halfUp,
            center - halfRight + halfUp
        };

        Handles.BeginGUI();

        Color previousColor =
            Handles.color;

        Handles.color =
            new Color(
                0.28f,
                0.55f,
                0.25f
            );

        Handles.DrawAAConvexPolygon(
            points
        );

        Handles.color =
            previousColor;

        Handles.EndGUI();
    }

    private void DrawHole(
        Rect board,
        HoleDefinition hole)
    {
        Vector2 position =
            WorldToPreview(
                board,
                hole.Position
            );

        DrawCircle(
            position,
            hole.IsFreeStartHole
                ? 11f
                : 8f,
            new Color(
                0.18f,
                0.10f,
                0.05f
            )
        );
    }

    private void DrawScrew(
        Rect board,
        ScrewDefinition screw)
    {
        HoleDefinition hole =
            FindHole(
                screw.CurrentHoleId
            );

        if (hole == null)
            return;

        Vector2 position =
            WorldToPreview(
                board,
                hole.Position
            );

        DrawCircle(
            position,
            10f,
            new Color(
                0.82f,
                0.82f,
                0.82f
            )
        );
    }

    private HoleDefinition FindHole(
        int id)
    {
        foreach (HoleDefinition hole
                 in currentPuzzle.Holes)
        {
            if (hole.Id == id)
                return hole;
        }

        return null;
    }

    private Vector2 WorldToPreview(
        Rect board,
        Vector2 worldPosition)
    {
        float x =
            Mathf.InverseLerp(
                -3.5f,
                3.5f,
                worldPosition.x
            );

        float y =
            Mathf.InverseLerp(
                4.0f,
                -4.0f,
                worldPosition.y
            );

        return new Vector2(
            Mathf.Lerp(
                board.x + 20,
                board.xMax - 20,
                x
            ),

            Mathf.Lerp(
                board.y + 20,
                board.yMax - 20,
                y
            )
        );
    }

    private void DrawCircle(
        Vector2 center,
        float radius,
        Color color)
    {
        Handles.BeginGUI();

        Color previousColor =
            Handles.color;

        Handles.color =
            color;

        Handles.DrawSolidDisc(
            center,
            Vector3.forward,
            radius
        );

        Handles.color =
            previousColor;

        Handles.EndGUI();
    }
}
