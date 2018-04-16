using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skinner
{

    public class SkinnerTrail : MonoBehaviour
    {
        // ---------------
        #region External object/asset ref
        [Tooltip("Reference to an effect source")]
        [SerializeField]
        private SkinnerSource _source;

        public SkinnerSource source
        {
            get { return _source; }
            set { _source = value; _reconfigured = true; }
        }

        [Tooltip("Reference to a template object used for rendering trail lines")]
        [SerializeField]
        private SkinnerTailTemplate _template;
        #endregion

        // ---------------
        #region Dynamic settings
        public float speedLimit
        {
            get { return _speedLimit; }
            set { _speedLimit = value; }
        }

        [SerializeField]
        [Tooltip("Limits an amount of a vertex movement. This only affects changes" +
            "in vertex positions (doesn't change velocity vectors)")]
        private float _speedLimit = 0.4f;

        // Drag coefficient 
        public float drag
        {
            get { return _drag; }
            set { _drag = value; }
        }

        [SerializeField]
        [Tooltip("Drag coefficient (damping coefficient)")]
        float _drag = 5;
        #endregion

        #region Line width modifier

        // Part of lines under this peed will be culled.
        [SerializeField]
        [Tooltip("Part of lines under this peed will be culled.")]
        private float _cutoffSpeed = 0.0f;

        public float cutoffSpeed
        {
            get { return _cutoffSpeed; }
            set { _cutoffSpeed = value; }
        }

        // Increase the line width based on this speed.
        [SerializeField]
        [Tooltip("Increase the line width based on this speed.")]
        private float _speedToWidth = 0.02f;

        public float speedToWidth
        {
            get { return _speedToWidth; }
            set { _speedToWidth = value; }
        }

        // Maximum width of lines.
        [SerializeField]
        [Tooltip("Maximum width of lines.")]
        private float _maxWidth = 0.05f;

        public float maxWidth
        {
            get { return _maxWidth; }
            set { _maxWidth = value; }
        }

        #endregion

        // ---------------
        #region Other settings
        [SerializeField]
        [Tooltip("Determines the random number sequence used for the effect")]
        private int _randomSeed = 0;

        public int randomSeed
        {
            get { return _randomSeed; }
            set { _randomSeed = value; _reconfigured = true; }
        }
        #endregion

        // ---------------
        #region Reconfiguration detection
        private bool _reconfigured;
        #endregion

        // ---------------
        #region Built-in assets
        [SerializeField]
        private Shader _kernelShader;
        [SerializeField]
        private Material _defaultMaterial;
        #endregion

        // ---------------
        #region Animation kernels management
        enum Kernels
        {
            InitializePosition,
            InitializeVelocity,
            InitializeOrthnorm,
            UpdatePosition,
            UpdateVelocity,
            UpdateOrthnorm
        }

        enum Buffers
        {
            Position,
            Velocity,
            Orthnorm
        }

        private AnimationKernelSet<Kernels, Buffers> _kernel;

        private void InvokeAnimationKernels()
        {
            if (_kernel == null)
                _kernel = new AnimationKernelSet<Kernels, Buffers>(_kernelShader, x => (int)x, x => (int)x);

            if (!_kernel.ready)
            {
                // Initialize the animation kernels and buffers.
                _kernel.Setup(_source.vertexCount, _template.historyLength);
                _kernel.material.SetTexture("_SourcePositionBuffer1", _source.positionBuffer);
                _kernel.material.SetFloat("_RandomSeed", _randomSeed);
                _kernel.Invoke(Kernels.InitializePosition, Buffers.Position);
                _kernel.Invoke(Kernels.InitializeVelocity, Buffers.Velocity);
                _kernel.Invoke(Kernels.InitializeOrthnorm, Buffers.Orthnorm);
            }
            else
            {
                // Transfer the source position attributes.
                _kernel.material.SetTexture("_SourcePositionBuffer0", _source.previousPositionBuffer);
                _kernel.material.SetTexture("_SourcePositionBuffer1", _source.positionBuffer);

                // Invoke the velocity update kernel.
                _kernel.material.SetTexture("_PositionBuffer", _kernel.GetLastBuffer(Buffers.Position));
                _kernel.material.SetTexture("_VelocityBuffer", _kernel.GetLastBuffer(Buffers.Velocity));
                _kernel.material.SetFloat("_SpeedLimit", _speedLimit);
                _kernel.Invoke(Kernels.UpdateVelocity, Buffers.Velocity);

                // Invoke the position update kernels with the updated velocity.
                _kernel.material.SetTexture("_VelocityBuffer", _kernel.GetWorkingBuffer(Buffers.Velocity));
                _kernel.material.SetFloat("_Drag", Mathf.Exp(-_drag * Time.deltaTime));
                _kernel.Invoke(Kernels.UpdatePosition, Buffers.Position);

                // Invoke the orthonormal update kernel with the updated velocity.
                _kernel.material.SetTexture("_PositionBuffer", _kernel.GetWorkingBuffer(Buffers.Position));
                _kernel.material.SetTexture("_OrthnormBuffer", _kernel.GetLastBuffer(Buffers.Orthnorm));
                _kernel.Invoke(Kernels.UpdateOrthnorm, Buffers.Orthnorm);
            }

            _kernel.SwapBuffers();
        }
        #endregion

        // ---------------
        #region External renderer control

        private RendererAdapter _renderer;

        private void UpdateRenderer()
        {
            if (_renderer == null)
                _renderer = new RendererAdapter(gameObject, _defaultMaterial);

            // Update the custom property block.
			// DONE :: Q :: What kind of shader it's using here? Maybe TrailSurface.cginc.
			// → SetPropertyBlockでDefaultMaterialを付加している
            var block = _renderer.propertyBlock;
            block.SetTexture("_PreviousPositionBuffer", _kernel.GetWorkingBuffer(Buffers.Position));
			// Q :: There is no "_PreviousVelocityBuffer" in TrailSurface.cginc file.
			block.SetTexture("_PreviousVelocityBuffer", _kernel.GetWorkingBuffer(Buffers.Velocity));
            block.SetTexture("_PreviousOrthnormBuffer", _kernel.GetWorkingBuffer(Buffers.Orthnorm));
            block.SetTexture("_PositionBuffer", _kernel.GetLastBuffer(Buffers.Position));
            block.SetTexture("_VelocityBuffer", _kernel.GetLastBuffer(Buffers.Velocity));
            block.SetTexture("_OrthnormBuffer", _kernel.GetLastBuffer(Buffers.Orthnorm));
            block.SetVector("_LineWidth", new Vector3(_maxWidth, _cutoffSpeed, _speedToWidth / _maxWidth));
            block.SetFloat("_RandomSeed", _randomSeed);

            _renderer.Update(_template.mesh);
        }
        #endregion

        // ---------------
        #region MonoBehaiviour Functions
        private void Reset()
        {
            _reconfigured = true;
        }

        private void OnDestroy()
        {
            _kernel.Release();
        }

        // Called when this script is loaded or some values are changed through inspector.
        private void OnValidate()
        {
			// Change values;
			_cutoffSpeed = Mathf.Max(_cutoffSpeed, 0);
			_speedToWidth = Mathf.Max(_speedToWidth, 0);
			_maxWidth = Mathf.Max(_maxWidth, 0);
        }

        private void LateUpdate()
        {
            // Skip all procedures if the source is not ready.
            if (_source == null || !_source) return;

            // Reset the animation kernels on reconfiguration.
            if (_reconfigured)
            {
                if (_kernel != null) _kernel.Release();
                _reconfigured = false;
            }

            // Invoke the animation kernels and update the renderer.
            InvokeAnimationKernels();
            UpdateRenderer();
        }
        #endregion
    }

}