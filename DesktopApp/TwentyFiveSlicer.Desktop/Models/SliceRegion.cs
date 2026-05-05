namespace TwentyFiveSlicer.Desktop.Models;

public readonly record struct SliceRegion(FloatRect Source, FloatRect Destination, int Column, int Row);