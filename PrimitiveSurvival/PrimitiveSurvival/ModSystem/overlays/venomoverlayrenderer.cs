namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Client;
    using System;
    //using System.Diagnostics;


    /*
      https://github.com/anegostudios/vsmodexamples/tree/master/Mods/ScreenOverlayShaderExample
      https://gist.github.com/patriciogonzalezvivo/670c22f3966e662d2f83
      http://glslsandbox.com/e#71442.0
      https://www.geeks3d.com/20091020/shader-library-lens-circle-post-processing-effect-glsl/
      */

    public class VenomOverlayRenderer : IRenderer
    {
        private readonly MeshRef quadRef;
        private readonly ICoreClientAPI capi;

        public IShaderProgram overlayShaderProg;

        private readonly VenomOverlayRenderer renderer;
        private float venomCounter;

        protected static readonly Random Rnd = new Random();

        public VenomOverlayRenderer(ICoreClientAPI capi)
        {
            this.capi = capi;
            this.venomCounter = 20;
            var quadMesh = QuadMeshUtil.GetCustomQuadModelData(-1, -1, 0, 2, 2);
            quadMesh.Rgba = null;
            this.quadRef = capi.Render.UploadMesh(quadMesh);
            capi.Event.ReloadShader += this.LoadShader;
            capi.Event.RegisterRenderer(this, EnumRenderStage.Ortho, "poison");
            capi.Event.LeaveWorld += () => this.renderer?.Dispose();
        }

        public double RenderOrder => 1.2;

        public int RenderRange => 1;

        public void Dispose()
        {
            this.capi.Render.DeleteMesh(this.quadRef);
            this.overlayShaderProg.Dispose();
        }


        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            var isVenomed = VenomState.Venomed;
            var player = this.capi.World.Player.Entity.GetName();
            if (isVenomed != player)
            {
                return;
            }
            this.venomCounter -= 1f * deltaTime;
            this.capi.World.SetCameraShake(0.2f);
            var curShader = this.capi.Render.CurrentActiveShader;
            curShader.Stop();

            this.overlayShaderProg.Use();
            this.capi.Render.GlToggleBlend(true);
            var elapsedTime = this.capi.World.ElapsedMilliseconds / 1000f;
            this.overlayShaderProg.Uniform("time", elapsedTime);
            this.capi.Render.RenderMesh(this.quadRef);
            this.overlayShaderProg.Stop();
            curShader.Use();

            if (this.venomCounter < 0)
            {
                float done = Rnd.Next(1000);
                if (done < 1)
                {
                    this.capi.World.SetCameraShake(0);
                    VenomState.Venomed = "";
                    this.venomCounter = 20;
                }
            } //venomed is the player's name
        }

        public bool LoadShader()
        {
            this.overlayShaderProg = this.capi.Shader.NewShaderProgram();
            this.overlayShaderProg.VertexShader = this.capi.Shader.NewShader(EnumShaderType.VertexShader);
            this.overlayShaderProg.FragmentShader = this.capi.Shader.NewShader(EnumShaderType.FragmentShader);
            this.capi.Shader.RegisterFileShaderProgram("poison", this.overlayShaderProg);
            this.overlayShaderProg.Compile();
            return true;
        }
    }
}
