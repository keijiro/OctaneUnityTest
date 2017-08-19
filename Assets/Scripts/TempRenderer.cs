using UnityEngine;

// Temporary renderer object used to feed dynamic meshs/materials to Octane.

[ExecuteInEditMode]
public class TempRenderer : MonoBehaviour
{
    #region Allocation/release

    static public TempRenderer Allocate()
    {
        foreach (var r in FindObjectsOfType<TempRenderer>())
            if (r.TryAllocate()) return r;
        return null;
    }

    public void Release()
    {
        if (_meshFilter != null) _meshFilter.sharedMesh = null;
        if (_meshRenderer != null) _meshRenderer.sharedMaterial = null;
        _available = true;
    }

    #endregion

    #region Public methods

    public Mesh mesh {
        get { return _mesh; }
        set { _meshFilter.sharedMesh = _mesh = value; }
    }

    public Material material {
        get { return _material; }
        set { _meshRenderer.sharedMaterial = _material = value; }
    }

    public void SetTransform(Transform tr)
    {
        transform.position = tr.position;
        transform.rotation = tr.rotation;
        transform.localScale = tr.localScale;
    }

    #endregion

    #region Private fields

    // References to components.
    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;

    // Default empty assets.
    Mesh _defaultMesh;
    Material _defaultMaterial;

    // Given assets.
    Mesh _mesh;
    Material _material;

    // Is this renderer available to use?
    bool _available;

    bool TryAllocate()
    {
        if (!_available) return false;
        _available = false;
        return true;
    }

    #endregion

    #region MonoBehaviour functions

    void Awake()
    {
        // Initialize the components.
        // It could have the components before awake in some cases (e.g. object
        // was duplicated from existing one), so we add these components only
        // when they don't exist.
        _meshFilter = GetComponent<MeshFilter>();

        if (_meshFilter == null)
        {
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            _meshFilter.hideFlags = HideFlags.HideAndDontSave;
        }

        _meshRenderer = GetComponent<MeshRenderer>();

        if (_meshRenderer == null)
        {
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            _meshRenderer.hideFlags = HideFlags.HideAndDontSave;
        }

        // Create empty assets and set them to the components.
        // This is needed to avoid null-ref error in Octane plugin.
        _defaultMesh = new Mesh();
        _defaultMesh.hideFlags = HideFlags.HideAndDontSave;
        _meshFilter.sharedMesh = _defaultMesh;

        _defaultMaterial = new Material(Shader.Find("Standard"));
        _defaultMaterial.hideFlags = HideFlags.HideAndDontSave;
        _meshRenderer.sharedMaterial = _defaultMaterial;

        // Now it's available to use.
        _available = true;
    }

    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            Destroy(_defaultMesh);
            Destroy(_defaultMaterial);
        }
        else
        {
            DestroyImmediate(_defaultMesh);
            DestroyImmediate(_defaultMaterial);
        }
    }

    #endregion
}
