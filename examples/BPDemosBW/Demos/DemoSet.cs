using DemoContentLoader;
using DemoRenderer;
using Demos.Demos;
using Demos.Demos.Cars;
using Demos.Demos.Characters;
using Demos.Demos.Dancers;
using Demos.Demos.Sponsors;
using Demos.Demos.Tanks;
using System;
using System.Collections.Generic;
using System.Text;
using Blazor.Extensions.Canvas.WebGL;

namespace Demos
{
    /// <summary>
    /// Constructs a demo from the set of available demos on demand.
    /// </summary>
    public class DemoSet
    {
        struct Option
        {
            public string Name;
            public Func<WebGLContext, ContentArchive, Camera, Task<Demo>> Builder;
        }

        List<Option> options = new();
        void AddOption<T>() where T : Demo, new()
        {
            options.Add(new Option
            {
                Builder = async (context, content, camera) =>
                {
                    //Note that the actual work is done in the Initialize function rather than a constructor.
                    //The 'new T()' syntax actually uses reflection and repackages exceptions in an inconvenient way.
                    //By using Initialize instead, the stack trace and debugger will go right to the source.
                    var demo = new T();
                    await demo.LoadGraphicalContentAsync(context, content);
                    demo.Initialize(content, camera);
                    return demo;
                },
                Name = typeof(T).Name
            });
        }

        public DemoSet()
        {
            AddOption<CarDemo>();
            AddOption<TankDemo>();
            AddOption<CharacterDemo>();
            AddOption<RagdollTubeDemo>();
            AddOption<PyramidDemo>();
            AddOption<ColosseumDemo>();
            AddOption<NewtDemo>();
            AddOption<ClothDemo>();
            AddOption<DancerDemo>();
            AddOption<PlumpDancerDemo>();
            AddOption<ContinuousCollisionDetectionDemo>();
            AddOption<PlanetDemo>();
            AddOption<CompoundDemo>();
            AddOption<RopeStabilityDemo>();
            AddOption<SubsteppingDemo>();
            AddOption<ChainFountainDemo>();
            AddOption<RopeTwistDemo>();
            AddOption<BouncinessDemo>();
            AddOption<RayCastingDemo>();
            AddOption<SweepDemo>();
            AddOption<ContactEventsDemo>();
            AddOption<CollisionQueryDemo>();
            AddOption<SolverContactEnumerationDemo>();
            AddOption<CustomVoxelCollidableDemo>();
            AddOption<BlockChainDemo>();
            AddOption<SponsorDemo>();
        }

        public int Count { get { return options.Count; } }

        public string GetName(int index)
        {
            return options[index].Name;
        }

        public Task<Demo> BuildAsync(WebGLContext context, int index, ContentArchive content, Camera camera)
        {
            return options[index].Builder(context, content, camera);
        }
    }
}
