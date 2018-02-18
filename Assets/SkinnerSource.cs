using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skinner
{
    [AddComponentMenu("Skinner/Skinner Source")]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class SkinnerSource : MonoBehaviour
    {
        // ---------------
        #region Editable properties
        [Tooltip("Preprocessed model data.")]
        [SerializeField]
        private SkinnerModel _model;
        #endregion

        // ---------------
        #region Public properties
        public int vertexCount
        {
            get { return _model != null ? _model.vertexCount : 0; }
        }

        public bool isReady
        {
            get { return _frameCount > 1; }
        }

        // Baked texture of skinned vertex positions.
        public RenderTexture positionBuffer
        {
            get { return _swapFlag ? _positionBuffer1 : _positionBuffer0; }
        }

        public RenderTexture previousPositionBuffer
        {
            get { return _swapFlag ? _positionBuffer0 : _positionBuffer1; }
        }

        public RenderTexture normalBuffer
        {
            get { return _normalBuffer; }
        }

        public RenderTexture tangentBuffer
        {
            get { return _tangentBuffer; }
        }
        #endregion

        // ---------------
        #region Internal resources
        // Replacement shader used for baking vertex attributes.
        [SerializeField]
        private Shader _replacementShader;
        [SerializeField]
        private Shader _replacementShaderPosition;
        [SerializeField]
        private Shader _replacementShaderNormal;
        [SerializeField]
        private Shader _replacementShaderTangent;

        // Placeholder material draws nothing but only has the replacement tag.
        [SerializeField]
        private Material _placeholderMoterial;
        #endregion

        // ---------------
        #region Private memebers
        // Vertex attribute buffers
        private RenderTexture _positionBuffer0;
        private RenderTexture _positionBuffer1;
        private RenderTexture _normalBuffer;
        private RenderTexture _tangentBuffer;

        // Multiple render target for even/odd frames.
        private RenderBuffer[] _mrt0;
        private RenderBuffer[] _mrt1;
        private bool _swapFlag;

        // Temporary camera used for vertex baking.
        private Camera _camera;

        // Used for rejection the first and second frame.
        private int _frameCount;


        private RenderTexture CreateBuffer()
        {
            var format = SkinnerInternals.supportBufferFormat;
            var rt = new RenderTexture(_model.vertexCount, 1, 0, format);
            rt.filterMode = FilterMode.Point;
            return rt;
        }

        private void OverrideRenderer()
        {
            var smr = GetComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = _model.mesh;
            smr.material = _placeholderMoterial;
            smr.receiveShadows = false;

            /* 
             * This renderer is disabled to hide from other cameras. It will be
             * enabled by CullingStateController only while rendered from our 
             * vertex baking camera.
             */
            smr.enabled = false;
        }

        // Create a camera for vertex baking.
        private void BuildCamera()
        {
            // Create a new game obj
            var go = new GameObject("Camera");
            go.hideFlags = HideFlags.HideInHierarchy;

            var tr = go.transform;
            tr.parent = transform;
            tr.localPosition = Vector3.zero;
            tr.localRotation = Quaternion.identity;

            _camera = go.AddComponent<Camera>();

            _camera.renderingPath = RenderingPath.Forward;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.depth = -10000;

            _camera.nearClipPlane = -100;
            _camera.farClipPlane = 100;
            _camera.orthographic = true;
            _camera.orthographicSize = 100;

            _camera.enabled = false;

            var culler = go.AddComponent<CullingStateController>();
            culler.target = GetComponent<SkinnedMeshRenderer>();
        }
        #endregion

        // ---------------
        #region MonoBehaviour
        private void Start()
        {
            // Create the attribute buffers.
            _positionBuffer0 = CreateBuffer();
            _positionBuffer1 = CreateBuffer();
            _normalBuffer = CreateBuffer();
            _tangentBuffer = CreateBuffer();

            // MRT set 0 (used in even frames)
            _mrt0 = new[]
            {
                _positionBuffer0.colorBuffer,
                _normalBuffer.colorBuffer,
                _tangentBuffer.colorBuffer
            };

            // MRT set 1 (used in even frames)
            _mrt1 = new[]
            {
                _positionBuffer1.colorBuffer,
                _normalBuffer.colorBuffer,
                _tangentBuffer.colorBuffer
            };

            // Set up the baking rig
            OverrideRenderer();
            BuildCamera();

            _swapFlag = true; // This will be false at first update.
        }

        private void OnDestroy()
        {
            if (_positionBuffer0 != null) Destroy(_positionBuffer0);
            if (_positionBuffer1 != null) Destroy(_positionBuffer1);
            if (_normalBuffer != null) Destroy(_normalBuffer);
            if (_tangentBuffer != null) Destroy(_tangentBuffer);
        }

        private void LateUpdate()
        {
            // swap buffer on each frame
            _swapFlag = !_swapFlag;

            /*
             * Render to vertex attribute buffers at once with using MRT.
             * Note that we can't use MRT when VR is enalbed.
             * In this case, we'll use separate shaders to workaournd this issue.
             */
            if (!UnityEngine.XR.XRSettings.enabled)
            {
                if (_swapFlag)
                    _camera.SetTargetBuffers(_mrt1, _positionBuffer1.depthBuffer);
                else
                    _camera.SetTargetBuffers(_mrt0, _positionBuffer0.depthBuffer);
                _camera.RenderWithShader(_replacementShader, "Skinner");
            }
            else if (_swapFlag)
            {
                _camera.targetTexture = _positionBuffer1;
                _camera.RenderWithShader(_replacementShaderPosition, "Skinner");
                _camera.targetTexture = _normalBuffer;
                _camera.RenderWithShader(_replacementShaderNormal, "Skinner");
                _camera.targetTexture = _tangentBuffer;
                _camera.RenderWithShader(_replacementShaderTangent, "Skinner");
            }
            else
            {
                _camera.targetTexture = _positionBuffer0;
                _camera.RenderWithShader(_replacementShaderPosition, "Skinner");
                _camera.targetTexture = _normalBuffer;
                _camera.RenderWithShader(_replacementShaderNormal, "Skinner");
                _camera.targetTexture = _tangentBuffer;
                _camera.RenderWithShader(_replacementShaderTangent, "Skinner");
            }

            /*
             * We manually disable the skinned mesh renderer here because 
             * there is a regression from 2017.1.0 that prevents
             * CallingStateController from being called in OnPostRender.
             * This is pretty hackish workaround, so FIXME later.
             */
            GetComponent<SkinnedMeshRenderer>().enabled = false;

            _frameCount++;
        }
        #endregion
    }
}