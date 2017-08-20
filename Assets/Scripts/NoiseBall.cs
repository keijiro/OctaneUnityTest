using System.Collections.Generic;
using UnityEngine;
using Klak.Math;

[ExecuteInEditMode]
public class NoiseBall : MonoBehaviour
{
    #region Editable variables

    [SerializeField] int _triangleCount = 100;
    [SerializeField] float _triangleExtent = 0.1f;
    [SerializeField] float _shuffleSpeed = 4;
    [SerializeField] float _noiseAmplitude = 1;
    [SerializeField] float _noiseFrequency = 1;
    [SerializeField] Vector3 _noiseMotion = Vector3.up;
    [SerializeField] int _randomSeed = 0;
    [SerializeField] Material _material;

    #endregion

    #region Private variables

    TempRenderer _renderer;
    Mesh _mesh;

    float _time;
    Vector3 _noiseOffset;

    bool _rebuild;

    #endregion

    #region MonoBehaviour functions

    void OnValidate()
    {
        _triangleCount = Mathf.Max(0, _triangleCount);
        _triangleExtent = Mathf.Max(0, _triangleExtent);
        _noiseFrequency = Mathf.Max(0, _noiseFrequency);

        // Something might be changed: Rebuild the mesh in the next frame.
        _rebuild = true;
    }

    void OnEnable()
    {
        _rebuild = true;
    }

    void OnDisable()
    {
        ReleaseMesh();
    }

    void OnDestroy()
    {
        ReleaseMesh();
    }

    void Update()
    {
        // Play mode: Advance the time.
        if (Application.isPlaying)
        {
            _time += _shuffleSpeed * Time.deltaTime;
            _noiseOffset += _noiseMotion * Time.deltaTime;
            _rebuild = true;
        }

        // Reconstruct the mesh.
        if (_rebuild)
        {
            ReleaseMesh();
            BuildMesh();
            _rebuild = false;
        }

        // Update the renderer transform every frame.
        if (_renderer != null) _renderer.SetTransform(transform);
    }

    #endregion

    #region Mesh construction/destruction

    List<Vector3> _vbuffer = new List<Vector3>();
    List<Vector2> _uv = new List<Vector2>();
    List<int> _ibuffer = new List<int>();

    Vector3 RandomPoint(XXHash hash, int id)
    {
        float u = hash.Range(-Mathf.PI, Mathf.PI, id * 2);
        float z = hash.Range(-1.0f, 1.0f, id * 2 + 1);
        float l = Mathf.Sqrt(1 - z * z);
        return new Vector3(Mathf.Cos(u) * l, Mathf.Sin(u) * l, z);
    }

    void BuildMesh()
    {
        _vbuffer.Clear();
        _ibuffer.Clear();
        _uv.Clear();

        var hash = new Klak.Math.XXHash(1000);

        for (var i = 0; i < _triangleCount; i++)
        {
            var seed = (_randomSeed + Mathf.FloorToInt(i * 0.1f + _time)) * 10000;

            var i1 = i * 3;
            var i2 = i1 + 1;
            var i3 = i2 + 1;

            var v1 = RandomPoint(hash, i1 + seed);
            var v2 = RandomPoint(hash, i2 + seed);
            var v3 = RandomPoint(hash, i3 + seed);

            v2 = (v1 + (v2 - v1).normalized * _triangleExtent).normalized;
            v3 = (v1 + (v3 - v1).normalized * _triangleExtent).normalized;

            var l1 = Perlin.Noise(v1 * _noiseFrequency + _noiseOffset);
            var l2 = Perlin.Noise(v2 * _noiseFrequency + _noiseOffset);
            var l3 = Perlin.Noise(v3 * _noiseFrequency + _noiseOffset);

            l1 = Mathf.Abs(l1 * l1 * l1);
            l2 = Mathf.Abs(l2 * l2 * l2);
            l3 = Mathf.Abs(l3 * l3 * l3);

            v1 *= 1 + l1 * _noiseAmplitude;
            v2 *= 1 + l2 * _noiseAmplitude;
            v3 *= 1 + l3 * _noiseAmplitude;

            _vbuffer.Add(v1);
            _vbuffer.Add(v2);
            _vbuffer.Add(v3);

            _uv.Add(Vector2.zero);
            _uv.Add(Vector2.zero);
            _uv.Add(Vector2.zero);

            _ibuffer.Add(i1);
            _ibuffer.Add(i2);
            _ibuffer.Add(i3);
        }

        _mesh = new Mesh();
        _mesh.hideFlags = HideFlags.DontSave;
        _mesh.SetVertices(_vbuffer);
        _mesh.SetUVs(0, _uv);
        _mesh.SetTriangles(_ibuffer, 0);
        _mesh.RecalculateNormals();

        _vbuffer.Clear();
        _ibuffer.Clear();

        _renderer = TempRenderer.Allocate();
        _renderer.mesh = _mesh;
        _renderer.material = _material;
    }

    void ReleaseMesh()
    {
        if (_renderer != null) _renderer.Release();

        if (_mesh != null)
        {
            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);
        }
    }

    #endregion
}
