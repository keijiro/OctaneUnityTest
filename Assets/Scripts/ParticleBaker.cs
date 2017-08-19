using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ParticleBaker : MonoBehaviour
{
    #region Editable variables

    [SerializeField] ParticleSystem _target;

    #endregion

    #region Private variables

    Mesh _mesh;
    TempRenderer _renderer;
    float _lastUpdateTime = -1;

    #endregion

    #region MonoBehaviour functions

    void OnDestroy()
    {
        if (_mesh != null)
        {
            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);
        }

        if (_renderer) _renderer.Release();
    }

    void LateUpdate()
    {
        // Do nothing if no target is given.
        if (_target == null) return;

        // Allocate a temporary renderer if not yet.
        if (_renderer == null) _renderer = TempRenderer.Allocate();

        // Update the mesh object if the simulation time is updated.
        if (_lastUpdateTime != _target.time) UpdateMesh();

        // Set the mesh/material to the remporary renderer.
        var pr = _target.GetComponent<ParticleSystemRenderer>();
        _renderer.SetRenderProperties(_mesh, pr.sharedMaterial);
        _renderer.SetTransform(transform);
    }

    #endregion

    #region Editable fields

    // Arrays/lists used to bake particles.
    // These arrays/lists are reused between frames to reduce memory pressure.

    ParticleSystem.Particle[] _particleBuffer;

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

    // Update the mesh object.
    // Destroy the old mesh, then create a new mesh and bake into it.
    void UpdateMesh()
    {
        if (_mesh != null)
        {
            if (Application.isPlaying)
                Destroy(_mesh);
            else
                DestroyImmediate(_mesh);
        }

        _mesh = new Mesh();
        _mesh.hideFlags = HideFlags.HideAndDontSave;

        BakeIntoMesh();

        _lastUpdateTime = _target.time;
    }

    // Bake the target particle system into the mesh object.
    void BakeIntoMesh()
    {
        var main = _target.main;

        if (_particleBuffer == null ||
            _particleBuffer.Length != main.maxParticles)
        {
            _particleBuffer = new ParticleSystem.Particle[main.maxParticles];
        }

        var count = _target.GetParticles(_particleBuffer);

        var renderer = _target.GetComponent<ParticleSystemRenderer>();
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
            var p = _particleBuffer[i];

            var mtx = Matrix4x4.TRS(
                p.position,
                Quaternion.AngleAxis(p.rotation, p.axisOfRotation),
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
        _mesh.SetUVs(0, _uv0_out);
        _mesh.SetTriangles(_idx_out, 0, true);
    }

    #endregion
}
