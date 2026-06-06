namespace Cpu.Tui.Graphics;

public readonly record struct Vector3(float X, float Y, float Z)
{
    public static Vector3 operator*(Vector3 v, Matrix4 m) => new(
        v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31 + m.M41,
        v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32 + m.M42,
        v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33 + m.M43);
}

public readonly record struct Matrix4(
    float M11, float M12, float M13, float M14,
    float M21, float M22, float M23, float M24,
    float M31, float M32, float M33, float M34,
    float M41, float M42, float M43, float M44)
{
    public static Matrix4 RotateX(float a)
    {
        float c = MathF.Cos(a), s = MathF.Sin(a);
        return new Matrix4(1, 0, 0, 0, 0, c, -s, 0, 0, s, c, 0, 0, 0, 0, 1);
    }

    public static Matrix4 RotateY(float a)
    {
        float c = MathF.Cos(a), s = MathF.Sin(a);
        return new Matrix4(c, 0, s, 0, 0, 1, 0, 0, -s, 0, c, 0, 0, 0, 0, 1);
    }

    public static Matrix4 RotateZ(float a)
    {
        float c = MathF.Cos(a), s = MathF.Sin(a);
        return new Matrix4(c, -s, 0, 0, s, c, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
    }

    public static Matrix4 operator*(Matrix4 a, Matrix4 b) => new(
        a.M11*b.M11 + a.M12*b.M21 + a.M13*b.M31 + a.M14*b.M41,
        a.M11*b.M12 + a.M12*b.M22 + a.M13*b.M32 + a.M14*b.M42,
        a.M11*b.M13 + a.M12*b.M23 + a.M13*b.M33 + a.M14*b.M43,
        a.M11*b.M14 + a.M12*b.M24 + a.M13*b.M34 + a.M14*b.M44,
        a.M21*b.M11 + a.M22*b.M21 + a.M23*b.M31 + a.M24*b.M41,
        a.M21*b.M12 + a.M22*b.M22 + a.M23*b.M32 + a.M24*b.M42,
        a.M21*b.M13 + a.M22*b.M23 + a.M23*b.M33 + a.M24*b.M43,
        a.M21*b.M14 + a.M22*b.M24 + a.M23*b.M34 + a.M24*b.M44,
        a.M31*b.M11 + a.M32*b.M21 + a.M33*b.M31 + a.M34*b.M41,
        a.M31*b.M12 + a.M32*b.M22 + a.M33*b.M32 + a.M34*b.M42,
        a.M31*b.M13 + a.M32*b.M23 + a.M33*b.M33 + a.M34*b.M43,
        a.M31*b.M14 + a.M32*b.M24 + a.M33*b.M34 + a.M34*b.M44,
        a.M41*b.M11 + a.M42*b.M21 + a.M43*b.M31 + a.M44*b.M41,
        a.M41*b.M12 + a.M42*b.M22 + a.M43*b.M32 + a.M44*b.M42,
        a.M41*b.M13 + a.M42*b.M23 + a.M43*b.M33 + a.M44*b.M43,
        a.M41*b.M14 + a.M42*b.M24 + a.M43*b.M34 + a.M44*b.M44);
}

public sealed class Demo3D
{
    private int _frame;
    public int Frame => _frame;
    private int _shapeIndex;
    private Vector3[] _vertices = [];
    private (int A, int B)[] _edges = [];
    private string _name = "";

    public float Scale { get; set; } = 60f;
    public float ViewDist { get; set; } = 4f;
    public float AngleXOffset { get; set; }
    public float AngleYOffset { get; set; }
    public float AngleZOffset { get; set; }

    public void ZoomIn() { Scale = Math.Min(Scale * 1.2f, 300f); }
    public void ZoomOut() { Scale = Math.Max(Scale / 1.2f, 5f); }
    public void RotateUp() { AngleXOffset += 0.2f; }
    public void RotateDown() { AngleXOffset -= 0.2f; }
    public void RotateLeft() { AngleYOffset -= 0.2f; }
    public void RotateRight() { AngleYOffset += 0.2f; }

    public string Name => _name;
    public int ShapeIndex => _shapeIndex;

    public Demo3D()
    {
        BuildCube();
    }

    public void NextShape()
    {
        _shapeIndex = (_shapeIndex + 1) % 3;
        switch (_shapeIndex)
        {
            case 0: BuildCube(); break;
            case 1: BuildPyramid(); break;
            case 2: BuildSphere(); break;
        }
    }

    public void BuildCube()
    {
        _name = "Cube";
        _vertices =
        [
            new(-1,-1,-1), new( 1,-1,-1), new( 1, 1,-1), new(-1, 1,-1),
            new(-1,-1, 1), new( 1,-1, 1), new( 1, 1, 1), new(-1, 1, 1)
        ];
        _edges =
        [
            (0,1),(1,2),(2,3),(3,0),(4,5),(5,6),(6,7),(7,4),
            (0,4),(1,5),(2,6),(3,7)
        ];
    }

    public void BuildPyramid()
    {
        _name = "Pyramid";
        float h = 1.5f;
        _vertices =
        [
            new(-1, -h, -1), new( 1, -h, -1), new( 1, -h,  1), new(-1, -h,  1),
            new( 0,  h,  0)
        ];
        _edges =
        [
            (0,1),(1,2),(2,3),(3,0),
            (0,4),(1,4),(2,4),(3,4)
        ];
    }

    public void BuildSphere()
    {
        _name = "Sphere";
        int segs = 12;
        int rings = 8;
        var verts = new List<Vector3>();
        var edgeList = new List<(int, int)>();

        for (int ring = 0; ring <= rings; ring++)
        {
            float theta = ring * MathF.PI / rings;
            for (int seg = 0; seg < segs; seg++)
            {
                float phi = seg * 2 * MathF.PI / segs;
                verts.Add(new(
                    MathF.Sin(theta) * MathF.Cos(phi),
                    MathF.Cos(theta),
                    MathF.Sin(theta) * MathF.Sin(phi)));
            }
        }

        for (int ring = 0; ring < rings; ring++)
        {
            for (int seg = 0; seg < segs; seg++)
            {
                int cur = ring * segs + seg;
                int next = ring * segs + (seg + 1) % segs;
                int below = (ring + 1) * segs + seg;
                int belowNext = (ring + 1) * segs + (seg + 1) % segs;
                edgeList.Add((cur, next));
                edgeList.Add((cur, below));
                if (ring == rings - 1)
                    edgeList.Add((below, belowNext));
            }
        }

        _vertices = verts.ToArray();
        _edges = edgeList.ToArray();
    }

    public void Tick(PixelCanvas canvas)
    {
        canvas.Clear(Pixel.Black);
        _frame++;

        float angleX = AngleXOffset + _frame * 0.02f;
        float angleY = AngleYOffset + _frame * 0.03f + _shapeIndex;
        float angleZ = AngleZOffset + _frame * 0.01f;

        var rot = Matrix4.RotateX(angleX) * Matrix4.RotateY(angleY) * Matrix4.RotateZ(angleZ);

        int cx = canvas.Width / 2;
        int cy = canvas.Height / 2;

        var projected = new (int X, int Y)[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            Vector3 v = _vertices[i] * rot;
            float factor = Scale / (v.Z + ViewDist);
            int sx = cx + (int)(v.X * factor);
            int sy = cy - (int)(v.Y * factor);
            projected[i] = (sx, sy);
        }

        // Draw edges
        Pixel color = _shapeIndex switch
        {
            0 => new Pixel(100, 200, 255), // cyan for cube
            1 => new Pixel(255, 200, 50),  // yellow for pyramid
            _ => new Pixel(255, 100, 100), // red for sphere
        };

        foreach (var (a, b) in _edges)
        {
            if (a < projected.Length && b < projected.Length)
                canvas.DrawLine(projected[a].X, projected[a].Y, projected[b].X, projected[b].Y, color);
        }
    }
}
