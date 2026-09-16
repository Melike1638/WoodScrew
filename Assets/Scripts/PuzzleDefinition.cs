using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PuzzleDefinition
{
    public int LevelNumber;

    public List<PlankDefinition> Planks =
        new List<PlankDefinition>();

    public List<HoleDefinition> Holes =
        new List<HoleDefinition>();

    public List<ScrewDefinition> Screws =
        new List<ScrewDefinition>();
}

[Serializable]
public class PlankDefinition
{
    public int Id;

    public Vector2 Position;
    public Vector2 Size;

    public float Rotation;

    public int ScrewAId;
    public int ScrewBId;
    public int ScrewCId = -1;
}

[Serializable]
public class HoleDefinition
{
    public int Id;

    public Vector2 Position;

    public bool IsFreeStartHole;
}

[Serializable]
public class ScrewDefinition
{
    public int Id;

    public int OwnerPlankId;

    public int OriginalHoleId;
    public int CurrentHoleId;
}