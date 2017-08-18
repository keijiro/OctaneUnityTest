using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class ParticleBaker : MonoBehaviour
{
    [SerializeField] ParticleSystem _target;

    Mesh _mesh;
    ParticleSystem.Particle[] _buffer;
    float _lastUpdateTime = -1;

    void Update()
    {
        if (_target == null) return;

        if (_lastUpdateTime != _target.time) UpdateMesh();

        var filter = GetComponent<MeshFilter>();
        if (filter == null)
        {
            filter = gameObject.AddComponent<MeshFilter>();
            filter.hideFlags = HideFlags.NotEditable;
        }

        filter.sharedMesh = _mesh;
    }

    void UpdateMesh()
    {
        if (_mesh != null)
            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);

        _mesh = new Mesh();
        _mesh.hideFlags = HideFlags.HideAndDontSave;

        BakeMesh();

        _lastUpdateTime = _target.time;
    }

    List<Vector3> _vtx_in = new List<Vector3>();
    List<Vector3> _nrm_in = new List<Vector3>();
    List<Vector4> _tan_in = new List<Vector4>();
    List<Vector2> _uv0_in = new List<Vector2>();
    List<int> _idx_in = new List<int>();

    List<Vector3> _vtx_out = new List<Vector3>();
    List<Vector3> _nrm_out = new List<Vector3>();
    List<Vector4> _tan_out = new List<Vector4>();
    List<Vector2> _uv0_out = new List<Vector2>();
    List<int> _idx_out = new List<int>();

    void BakeMesh()
    {
        var main = _target.main;
        var renderer = _target.GetComponent<ParticleSystemRenderer>();

        if (_buffer == null || _buffer.Length != main.maxParticles)
            _buffer = new ParticleSystem.Particle[main.maxParticles];

        var count = _target.GetParticles(_buffer);
        var template = renderer.mesh;

        template.GetVertices(_vtx_in);
        template.GetNormals(_nrm_in);
        template.GetTangents(_tan_in);
        template.GetUVs(0, _uv0_in);
        template.GetIndices(_idx_in, 0);

        _vtx_out.Clear();
        _nrm_out.Clear();
        _tan_out.Clear();
        _uv0_out.Clear();
        _idx_out.Clear();

        for (var i = 0; i < count; i++)
        {
            var p = _buffer[i];

            var mtx = Matrix4x4.TRS(
                p.position,
                Quaternion.Euler(p.rotation3D),
                Vector3.one * p.GetCurrentSize(_target)
            );

            var vi0 = _vtx_out.Count;

            foreach (var v in _vtx_in) _vtx_out.Add(mtx.MultiplyPoint(v));
            foreach (var n in _nrm_in) _nrm_out.Add(mtx.MultiplyVector(n));

            foreach (var t in _tan_in)
            {
                var mt = mtx.MultiplyVector(t);
                _tan_out.Add(new Vector4(mt.x, mt.y, mt.z, t.w));
            }

            _uv0_out.AddRange(_uv0_in);

            foreach (var idx in _idx_in) _idx_out.Add(idx + vi0);
        }

        _mesh.SetVertices(_vtx_out);
        _mesh.SetNormals(_nrm_out);
        _mesh.SetTangents(_tan_out);
        if (_uv0_out.Count > 0) _mesh.SetUVs(0, _uv0_out);
        _mesh.SetTriangles(_idx_out, 0, true);
    }
}
