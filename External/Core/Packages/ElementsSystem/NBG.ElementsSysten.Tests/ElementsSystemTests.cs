using NBG.Core;
using NBG.Core.Events;
using NBG.Core.GameSystems;
using NBG.Entities;
using NUnit.Framework;
using Recoil;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NBG.ElementsSystem.Tests
{
    public class ElementsSystemTests
    {
        GameSystemWorld World;
        ElementsGameSystem _elementsSystem;

        GameObject testElementGO;
        TestElementObject testElement;

        GameObject heatableGO;
        IHeatable heatable;

        GameObject flammableGO;
        IFlammable flammable;

        GameObject extinguisherGO;
        IExtinguisher extinguisher;

        [SetUp]
        public void Setup()
        {
            ManagedWorld.Create(16);
            EntityStore.Create(10, 500);

            // Initialize game system world
            EventBus.Create();
            GameSystemWorldDefault.Create();
            World = GameSystemWorldDefault.Instance;

            _elementsSystem = World.GetOrCreateSystem<ElementsGameSystem>();
        }

        private void CreateTestElementsSystemObjects(ElementsGameSystem system)
        {
            testElementGO = new GameObject("ElementsSystemObject1");
            testElementGO.transform.position = new Vector3(-100, 0, 0);
            testElement = testElementGO.AddComponent<TestElementObject>();
            testElement.ElementId = system.GenerateUniqueElementsId();
            system.RegisterEvent<CustomElementEvent>();
            system.RegisterForProcessing(testElement);
            system.RegisterForEvents(testElement);
        }

        private void CreateHeatable()
        {
            heatableGO = new GameObject("Heatable");
            heatableGO.transform.position = new Vector3(-90, 0, 0);
            heatableGO.SetActive(false);
            heatable = HeatableElement.AddHeatableElementComponent(heatableGO, 0.5f, 1, 1, true, true, 2, true, 1, 0);
            ((IManagedBehaviour)heatable).OnLevelLoaded();
            ((IManagedBehaviour)heatable).OnAfterLevelLoaded();
            heatableGO.SetActive(true);
        }

        private void CreateFlammable()
        {
            flammableGO = new GameObject("Flammable");
            flammableGO.transform.position = new Vector3(-80, 0, 0);
            flammableGO.SetActive(false);
            flammable = FlammableElement.AddFlammableElementComponent(flammableGO, 0, 0, 0, 0, 0, true, true, 0.5f, true, 0, false, false);
            ((IManagedBehaviour)flammable).OnLevelLoaded();
            ((IManagedBehaviour)flammable).OnAfterLevelLoaded();
            flammableGO.SetActive(true);
        }

        private void CreateExtinguisher()
        {
            extinguisherGO = new GameObject("Extinguisher");
            extinguisherGO.transform.position = new Vector3(-70, 0, 0);
            extinguisherGO.SetActive(false);
            extinguisher = ExtinguisherElement.AddExtinguisherElementComponent(extinguisherGO, 1);
            ((IManagedBehaviour)extinguisher).OnLevelLoaded();
            ((IManagedBehaviour)extinguisher).OnAfterLevelLoaded();
            extinguisherGO.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (testElementGO != null)
            {
                Object.DestroyImmediate(testElementGO);
                testElementGO = null;
                testElement = null;
            }

            if (heatableGO != null)
            {
                ((IManagedBehaviour)heatable).OnLevelUnloaded();
                Object.DestroyImmediate(heatableGO);
                heatableGO = null;
                heatable = null;
            }

            if (flammableGO != null)
            {
                ((IManagedBehaviour)flammable).OnLevelUnloaded();
                Object.DestroyImmediate(flammableGO);
                flammableGO = null;
                flammable = null;
            }

            if (extinguisherGO != null)
            {
                ((IManagedBehaviour)extinguisher).OnLevelUnloaded();
                Object.DestroyImmediate(extinguisherGO);
                extinguisherGO = null;
                extinguisher = null;
            }

            BootManagedBehaviours.RunUnloadAll();

            _elementsSystem = null;

            // Shutdown game system world
            //Recoil.RecoilSystems.Shutdown();
            GameSystemWorldDefault.Destroy();
            EventBus.Destroy();

            // Shutdown entity world
            EntityStore.Destroy();
            ManagedWorld.Destroy();
        }

        [Test, Order(1)]
        public void CheckIfCustomElementsSystemEventsIsWorking()
        {
            CreateTestElementsSystemObjects(_elementsSystem);

            bool success = false;
            testElement.OnCustomElementsSystemEventReceived += (CustomElementEvent data) =>
            {
                success = true;
            };

            EventBus.Get().Send(new CustomElementEvent()
            {
                ElementId = testElement.ElementId,
                //GameObjectReference = testMaterial.gameObject,
            });

            Assert.IsTrue(success);
        }

        #region Heatable
        [UnityTest, Order(2)]
        public IEnumerator HeatableHeatAmountChanged()
        {
            CreateHeatable();
            yield return null;
            heatable.SetHeatChanged(0);

            float newHeatAmount = 0.5f;
            heatable.SetHeatChanged(newHeatAmount);

            Assert.IsTrue(heatable.HeatAmount == newHeatAmount);
        }

        [UnityTest, Order(3)]
        public IEnumerator HeatableHeatedUpCallback()
        {
            CreateHeatable();
            yield return null;
            heatable.SetHeatChanged(0);

            bool heatedCallbackFired = false;
            heatable.OnHeated += (withEffects) =>
            {
                heatedCallbackFired = true;
            };

            heatable.SetHeatChanged(1);

            Assert.IsTrue(heatedCallbackFired);
        }

        [UnityTest, Order(4)]
        public IEnumerator HeatableCooledDownCallback()
        {
            CreateHeatable();
            yield return null;
            heatable.SetHeatChanged(1);

            bool cooledDownCallbackFired = false;
            heatable.OnCooledDown += (withEffects) =>
            {
                cooledDownCallbackFired = true;
            };

            heatable.SetHeatChanged(0);

            Assert.IsTrue(cooledDownCallbackFired);
        }

        [UnityTest, Order(5)]
        public IEnumerator HeatableHeatAmountChangedCallback()
        {
            CreateHeatable();
            yield return null;
            heatable.SetHeatChanged(0.5f);

            bool onHeatAmountChangedCallbackFired = false;
            heatable.OnHeatAmountChanged += (heatAmount) =>
            {
                onHeatAmountChangedCallbackFired = true;
            };

            float newHeatAmount = 0;
            heatable.SetHeatChanged(newHeatAmount);

            Assert.IsTrue(onHeatAmountChangedCallbackFired);
        }
        #endregion

        #region Flammable
        [UnityTest, Order(6)]
        public IEnumerator FlammableIgnite()
        {
            CreateFlammable();
            yield return null;
            flammable.Extinguish(false);

            flammable.Ignite(false);
            Assert.IsTrue(flammable.IsBurning == true);
        }

        [UnityTest, Order(7)]
        public IEnumerator FlammableIgniteCallback()
        {
            CreateFlammable();
            yield return null;
            flammable.Extinguish(false);

            bool ignited = false;
            flammable.OnIgnited += (withEffects) =>
            {
                ignited = true;
            };

            flammable.Ignite(false);
            Assert.IsTrue(ignited);
        }

        [UnityTest, Order(8)]
        public IEnumerator FlammableExtinguish()
        {
            CreateFlammable();
            yield return null;
            flammable.Ignite(false);

            flammable.Extinguish(false);
            Assert.IsTrue(flammable.IsBurning == false);
        }

        [UnityTest, Order(9)]
        public IEnumerator FlammableExtinguishCallback()
        {
            CreateFlammable();
            yield return null;
            flammable.Ignite(false);

            bool extinguished = false;
            flammable.OnExtinguished += (withEffects) =>
            {
                extinguished = true;
            };

            flammable.Extinguish(false);
            Assert.IsTrue(extinguished);
        }

        [UnityTest, Order(10)]
        public IEnumerator FlammableFlameAmount()
        {
            CreateFlammable();
            yield return null;
            flammable.Ignite(true);

            float newFlameAmount = 0.5f;
            flammable.SetFlameAmount(newFlameAmount);
            Assert.IsTrue(flammable.FlameAmount == newFlameAmount);
        }

        [UnityTest, Order(11)]
        public IEnumerator FlammableFlameAmountCallback()
        {
            CreateFlammable();
            yield return null;
            flammable.Ignite(true);

            bool flameAmountChanged = false;
            flammable.OnBurningAmountChanged += (flameAmount) =>
            {
                flameAmountChanged = true;
            };

            flammable.SetFlameAmount(0.5f);
            Assert.IsTrue(flameAmountChanged);
        }
        #endregion
    }
}