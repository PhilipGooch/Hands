using NUnit.Framework;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace NBG.LogicGraph.CodeGen.Tests
{
    public class UserlandBindingsTestHelper
    {
        [NodeAPI("Test private")]
        private void Test_private()
        {
            Debug.Log($"{nameof(Test_private)} works");
        }

        [NodeAPI("Test protected")]
        protected void Test_protected()
        {
            Debug.Log($"{nameof(Test_protected)} works");
        }

        [NodeAPI("Test internal")]
        internal void Test_internal()
        {
            Debug.Log($"{nameof(Test_internal)} works");
        }

        [NodeAPI("Test 1 (void)")]
        public void Test1_void()
        {
            Debug.Log($"{nameof(Test1_void)} works");
        }

        [NodeAPI("Test 2 (void, int)")]
        public void Test2(int arg1)
        {
            Debug.Log($"{nameof(Test2)} works: {arg1}");
        }

        [NodeAPI("Test 3 (void, string)")]
        public void Test3_void_string(string arg1)
        {
            Debug.Log($"{nameof(Test3_void_string)} works: {arg1}");
        }

        [NodeAPI("Test 3 (void, bool)")]
        public void Test3_void_bool(bool arg1)
        {
            Debug.Log($"{nameof(Test3_void_bool)} works: {arg1}");
        }

        [NodeAPI("Test 3 (void, float)")]
        public void Test3_void_float(float arg1)
        {
            Debug.Log($"{nameof(Test3_void_float)} works: {arg1}");
        }

        [NodeAPI("Test 3 (void, Vector3)")]
        public void Test3_void_Vector3(Vector3 arg1)
        {
            Debug.Log($"{nameof(Test3_void_Vector3)} works: {arg1}");
        }

        [NodeAPI("Test 3 (void, UnityEngine.Object)")]
        public void Test3_void_UnityObject(UnityEngine.Object arg1)
        {
            Debug.Log($"{nameof(Test3_void_UnityObject)} works: {arg1}");
        }

        [NodeAPI("Test 3 (void, UnityEngine.Quaternion)")]
        public void Test3_void_Quaternion(UnityEngine.Quaternion arg1)
        {
            Debug.Log($"{nameof(Test3_void_Quaternion)} works: {arg1}");
        }

        [NodeAPI("Test 4 (all parameter types)")]
        public void Test4_all_parameter_types(bool arg1, int arg2, float arg3, string arg4, UnityEngine.Vector3 arg5, UnityEngine.Object arg6)
        {
            Debug.Log($"{nameof(Test4_all_parameter_types)} called with:\n{arg1}\n{arg2}\n{arg3}\n{arg4}\n{arg5}\n{arg6}");
        }

        [NodeAPI("Test 5 (returns arg1 * 2")]
        public int Test5_int_int(int arg1)
        {
            Debug.Log($"{nameof(Test5_int_int)} works: {arg1}");
            return arg1 * 2;
        }

        [NodeAPI("Test 6 (UnityEngine.Object derivatives)")]
        public void Test6_void_Component(UnityEngine.Component arg1)
        {
            Debug.Log($"{nameof(Test6_void_Component)} works: {arg1}");
        }

        [NodeBinding("CustomTest1_no_return_no_args", NodeBindingType.NBT_Call)]
        public static void CustomTest1_no_return_no_args(object instance, IStack context)
        {
            Debug.Log($"{nameof(CustomTest1_no_return_no_args)} works!");
        }

        [NodeAPI("Test 7 (void, out int)")]
        public void Test7_void_out_int(out int out1)
        {
            out1 = 7;
            Debug.Log($"{nameof(Test7_void_out_int)} works.");
        }

        [NodeAPI("Test 8 (void, out Vector3)")]
        public void Test8_void_out_Vector3(out UnityEngine.Vector3 out1)
        {
            out1 = new Vector3(1, 2, 3);
            Debug.Log($"{nameof(Test8_void_out_Vector3)} works.");
        }

        [NodeAPI("Test 9 (void, out UnityEngine.Object)")]
        public void Test9_void_out_Object(out UnityEngine.Object out1)
        {
            out1 = null;
            Debug.Log($"{nameof(Test9_void_out_Object)} works.");
        }

        [NodeAPI("Test 10 (void, out Quaternion)")]
        public void Test10_void_out_Quaternion(out Quaternion out1)
        {
            out1 = Quaternion.Euler(45, 0, 0);
            Debug.Log($"{nameof(Test10_void_out_Quaternion)} works.");
        }

        public int propertyValueInt;

        [NodeAPI("Test 11 int property")]
        public int ValueInt
        {
            get
            {
                Debug.Log($"{nameof(ValueInt)} get works.");
                return propertyValueInt;
            }

            set
            {
                Debug.Log($"{nameof(ValueInt)} set works.");
                propertyValueInt = value;
            }
        }

        [NodeAPI("Test 12 static int property")]
        public static int StaticValueInt
        {
            get
            {
                Debug.Log($"{nameof(StaticValueInt)} get works.");
                return 1;
            }
        }

        [NodeAPI("Test 13 (void, out Color)")]
        public void Test13_void_out_Color(out Color out1)
        {
            out1 = Color.red;
            Debug.Log($"{nameof(Test13_void_out_Color)} works.");
        }

        [NodeAPI("PropPubPriv")]
        public float PropPubPriv
        {
            get => 0.0f;
            private set { }
        }

        [NodeAPI("PropPrivPub")]
        public float PropPrivPub
        {
            private get => 0.0f;
            set { }
        }

        [NodeAPI("PropOnlyGet")]
        public float PropOnlyGet
        {
            get => 0.0f;
        }

        [NodeAPI("PropOnlySet")]
        public float PropOnlySet
        {
            set { }
        }
    }

    public class UserlandBindingsTestHelperDescendant : UserlandBindingsTestHelper
    {
        [NodeAPI("Test in descendant")]
        public void Test_in_descendant()
        {
            Debug.Log($"{nameof(Test_in_descendant)} works");
        }

    }

    public class UserlandBindingsTestScriptableObjectHelper : UnityEngine.ScriptableObject
    {
        public int x;
    }

    [NodeHideInUI]
    public static class UserlandBindingsTestExtensions
    {
        [NodeAPI("Test 1")]
        public static void Test1(this UserlandBindingsTestScriptableObjectHelper helper)
        {
            helper.x = 7;
            Debug.Log($"{nameof(Test1)} works.");

        }

        [NodeAPI("Test 2")]
        public static void Test2(this UserlandBindingsTestScriptableObjectHelper helper, int arg1)
        {
            helper.x = arg1;
            Debug.Log($"{nameof(Test2)} works: {arg1}.");

        }
    }

    public class UserlandBindingsTests
    {
        [Test]
        public void BindingsExist()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);
        }

        [Test]
        public void BindingsExistForChildTypes()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelperDescendant));
            Assert.IsNotNull(ubs);
        }

        [Test]
        public void BindingsFromChildDoNotExistInParent()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.SingleOrDefault(x => x.Name == nameof(UserlandBindingsTestHelperDescendant.Test_in_descendant));
            Assert.IsNull(ub);
        }

        [Test]
        public void PrivateBindingDoesNotExist()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.SingleOrDefault(x => x.Name == "UserlandBindingsTestHelper.Test_private");
            Assert.IsNull(ub);
        }

        [Test]
        public void ProtectedBindingDoesNotExist()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.SingleOrDefault(x => x.Name == "UserlandBindingsTestHelper.Test_protected");
            Assert.IsNull(ub);
        }

        [Test]
        public void InternalBindingDoesNotExist()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.SingleOrDefault(x => x.Name == nameof(UserlandBindingsTestHelper.Test_internal));
            Assert.IsNull(ub);
        }

        [Test]
        public void PropertyBindingDoesNotExists_PropPrivPub_get()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var name = "get_" + nameof(UserlandBindingsTestHelper.PropPrivPub);
            var ub = (UserlandMethodBinding)ubs.SingleOrDefault(x => x.Name == name);
            Assert.IsNull(ub);
        }

        [Test]
        public void PropertyBindingDoesNotExist_PropPrivPub_set()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var name = "set_" + nameof(UserlandBindingsTestHelper.PropPrivPub);
            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == name);
            Assert.IsNotNull(ub);
        }

        [Test]
        public void PropertyBindingExists_PropPubPriv_get()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var name = "get_" + nameof(UserlandBindingsTestHelper.PropPubPriv);
            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == name);
            Assert.IsNotNull(ub);
        }

        [Test]
        public void PropertyBindingDoesNotExist_PropPubPriv_set()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var name = "set_" + nameof(UserlandBindingsTestHelper.PropPubPriv);
            var ub = (UserlandMethodBinding)ubs.SingleOrDefault(x => x.Name == name);
            Assert.IsNull(ub);
        }

        [Test]
        public void PropertyBindingExists_PropOnlyGet()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var name = "get_" + nameof(UserlandBindingsTestHelper.PropOnlyGet);
            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == name);
            Assert.IsNotNull(ub);
        }

        [Test]
        public void PropertyBindingExists_PropOnlySet()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var name = "set_" + nameof(UserlandBindingsTestHelper.PropOnlySet);
            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == name);
            Assert.IsNotNull(ub);
        }

        [Test]
        public void BindingsWork_CALL_void_void()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test1_void));
            Assert.IsNotNull(ub);

            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test1_void)} works");

            if (ub != null)
            {
                var instance = new UserlandBindingsTestHelper();
                ub.Func(instance, null);
            }
        }

        [Test]
        public void BindingsWork_CALL_void_int()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test2));
            Assert.IsNotNull(ub);

            const int arg1 = 7;
            IStack st = new Stack();
            st.PushInt(arg1);

            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test2)} works: {arg1}");
            if (ub != null)
            {
                var instance = new UserlandBindingsTestHelper();
                ub.Func(instance, st);
            }
        }

        [Test]
        public void BindingsWork_CALL_void_int_with_incorrect_type_throws()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test2));
            Assert.IsNotNull(ub);
            if (ub == null)
                return;

            BindingsWork_CALL_void_int_with_incorrect_type_throws_Helper_bool(ub);
            BindingsWork_CALL_void_int_with_incorrect_type_throws_Helper_float(ub);
        }

        void BindingsWork_CALL_void_int_with_incorrect_type_throws_Helper_bool(UserlandMethodBinding ub)
        {
            const bool arg1 = false;
            IStack st = new Stack();
            st.PushBool(arg1);

            var instance = new UserlandBindingsTestHelper();
            try
            {
                ub.Func(instance, st);
                Assert.Fail("No exception detected");
            }
            catch (System.InvalidCastException)
            {
                Debug.Log("InvalidCastException received");
            }
            catch (InvalidVariableTypeException)
            {
                Debug.Log("InvalidVariableTypeException received");
            }
            catch (System.Exception e)
            {
                Assert.Fail($"Invalid exception detected: {e.GetType()}, {e.Message}");
            }
        }

        void BindingsWork_CALL_void_int_with_incorrect_type_throws_Helper_float(UserlandMethodBinding ub)
        {
            const float arg1 = 1.0f;
            IStack st = new Stack();
            st.PushFloat(arg1);

            var instance = new UserlandBindingsTestHelper();
            try
            {
                ub.Func(instance, st);
                Assert.Fail("No exception detected");
            }
            catch (System.InvalidCastException)
            {
                Debug.Log("InvalidCastException received");
            }
            catch (InvalidVariableTypeException)
            {
                Debug.Log("InvalidVariableTypeException received");
            }
            catch (System.Exception)
            {
                Assert.Fail("Invalid exception detected");
            }
        }

        [Test]
        public void BindingsWork_CALL_void_string()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test3_void_string));
            Assert.IsNotNull(ub);

            const string arg1 = "string test";
            IStack st = new Stack();
            st.PushString(arg1);

            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test3_void_string)} works: {arg1}");
            if (ub != null)
            {
                var instance = new UserlandBindingsTestHelper();
                ub.Func(instance, st);
            }
        }

        [Test]
        public void BindingsWork_CALL_void_bool()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test3_void_bool));
            Assert.IsNotNull(ub);

            const bool arg1 = true;
            IStack st = new Stack();
            st.PushBool(arg1);

            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test3_void_bool)} works: {arg1}");
            if (ub != null)
            {
                var instance = new UserlandBindingsTestHelper();
                ub.Func(instance, st);
            }
        }

        [Test]
        public void BindingsWork_CALL_void_float()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test3_void_float));
            Assert.IsNotNull(ub);

            const float arg1 = 1337.0f;
            IStack st = new Stack();
            st.PushFloat(arg1);

            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test3_void_float)} works: {arg1}");
            if (ub != null)
            {
                var instance = new UserlandBindingsTestHelper();
                ub.Func(instance, st);
            }
        }

        [Test]
        public void BindingsWork_CALL_void_Vector3()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test3_void_Vector3));
            Assert.IsNotNull(ub);

            Vector3 arg1 = new Vector3(1, 2, 3);
            IStack st = new Stack();
            st.PushVector3(arg1);

            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test3_void_Vector3)} works: {arg1}");
            if (ub != null)
            {
                var instance = new UserlandBindingsTestHelper();
                ub.Func(instance, st);
            }
        }

        [Test]
        public void BindingsWork_CALL_void_UnityObject()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test3_void_UnityObject));
            Assert.IsNotNull(ub);

            UnityEngine.Object arg1 = null;
            IStack st = new Stack();
            st.PushObject(arg1);

            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test3_void_UnityObject)} works: {arg1}");
            if (ub != null)
            {
                var instance = new UserlandBindingsTestHelper();
                ub.Func(instance, st);
            }
        }

        [Test]
        public void BindingsWork_CALL_void_UnityQuaternion()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test3_void_Quaternion));
            Assert.IsNotNull(ub);

            UnityEngine.Quaternion arg1 = Quaternion.Euler(45.0f, 0.0f, 0.0f);
            IStack st = new Stack();
            st.PushQuaternion(arg1);

            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test3_void_Quaternion)} works: {arg1}");
            if (ub != null)
            {
                var instance = new UserlandBindingsTestHelper();
                ub.Func(instance, st);
            }
        }

        [Test]
        public void BindingsWork_CALL_Test4_with_all_parameter_types()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test4_all_parameter_types));
            Assert.IsNotNull(ub);

            IStack st = new Stack();
            // Reverse order
            st.PushObject(null);
            st.PushVector3(new Vector3(1, 2, 3));
            st.PushString("str");
            st.PushFloat(3.33f);
            st.PushInt(2);
            st.PushBool(true);

            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test4_all_parameter_types)} called with:\nTrue\n2\n3.33\nstr\n(1.00, 2.00, 3.00)\n");
            if (ub != null)
            {
                var instance = new UserlandBindingsTestHelper();
                ub.Func(instance, st);
            }
        }

        [Test]
        public void BindingsWork_CALL_int_int()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test5_int_int));
            Assert.IsNotNull(ub);

            const int arg1 = 5;
            IStack st = new Stack();
            st.PushInt(arg1);

            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test5_int_int)} works: {arg1}");

            int ret = 0;
            if (ub != null)
            {
                var instance = new UserlandBindingsTestHelper();
                ub.Func(instance, st);

                ret = st.PopInt();
            }

            Assert.IsTrue(ret == arg1 * 2);
            Debug.Log($"ret = {ret}");
        }

        [Test]
        public void BindingsWork_CALL_void_UnityObject_derivative()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test6_void_Component));
            Assert.IsNotNull(ub);

            UnityEngine.Component arg1 = new GameObject().AddComponent<Rigidbody>();
            IStack st = new Stack();
            st.PushObject(arg1);

            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test6_void_Component)} works: {arg1}");
            if (ub != null)
            {
                var instance = new UserlandBindingsTestHelper();
                ub.Func(instance, st);
            }
        }

        [Test]
        public void BindingsWork_CALL_void_UnityObject_derivative_with_incorrect_type_throws()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test6_void_Component));
            Assert.IsNotNull(ub);
            if (ub == null)
                return;
            
            UnityEngine.ScriptableObject arg1 = UnityEngine.ScriptableObject.CreateInstance<UserlandBindingsTestScriptableObjectHelper>();
            IStack st = new Stack();
            st.PushObject(arg1);

            var instance = new UserlandBindingsTestHelper();
            try
            {
                ub.Func(instance, st);
                Assert.Fail("No exception detected");
            }
            catch (System.InvalidCastException)
            {
                Debug.Log("InvalidCastException received");
            }
            catch (System.Exception)
            {
                Assert.Fail("Invalid exception detected");
            }
        }

        [Test]
        public void BindingsMeta_correct_for_void_void()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test1_void));
            Assert.IsNotNull(ub);
            if (ub == null)
                return;

            Assert.IsTrue(ub.Source.Name == nameof(UserlandBindingsTestHelper.Test1_void));
        }

        [Test]
        public void Bindings_Custom_binding_is_enumerated()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandCustomMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.CustomTest1_no_return_no_args));
            Assert.IsNotNull(ub);
            if (ub == null)
                return;

            Assert.IsTrue(ub.Source.Name == nameof(UserlandBindingsTestHelper.CustomTest1_no_return_no_args));
        }

        [Test]
        public void BindingsWork_CALL_void_out_int()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test7_void_out_int));
            Assert.IsNotNull(ub);
            if (ub == null)
                return;

            
            IStack st = new Stack();
            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test7_void_out_int)} works.");
            var instance = new UserlandBindingsTestHelper();
            ub.Func(instance, st);
            var ret = st.PopInt();
            Assert.IsTrue(ret == 7);
        }

        [Test]
        public void BindingsWork_CALL_void_out_Vector3()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test8_void_out_Vector3));
            Assert.IsNotNull(ub);
            if (ub == null)
                return;


            IStack st = new Stack();
            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test8_void_out_Vector3)} works.");
            var instance = new UserlandBindingsTestHelper();
            ub.Func(instance, st);
            var ret = st.PopVector3();
            Assert.IsTrue(ret == new Vector3(1, 2, 3));
        }

        [Test]
        public void BindingsWork_CALL_void_out_UnityObject()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test9_void_out_Object));
            Assert.IsNotNull(ub);
            if (ub == null)
                return;


            IStack st = new Stack();
            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test9_void_out_Object)} works.");
            var instance = new UserlandBindingsTestHelper();
            ub.Func(instance, st);
            var ret = st.PopObject();
            Assert.IsTrue(ret == null);
        }

        [Test]
        public void BindingsWork_CALL_Test10_void_Quaternion()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test10_void_out_Quaternion));
            Assert.IsNotNull(ub);
            if (ub == null)
                return;

            IStack st = new Stack();
            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test10_void_out_Quaternion)} works.");
            var instance = new UserlandBindingsTestHelper();
            ub.Func(instance, st);
            var ret = st.PopQuaternion();
            Assert.IsTrue(ret == Quaternion.Euler(45.0f, 0.0f, 0.0f));
        }

        [Test]
        public void BindingsWork_CALL_EXTENSION_void_void()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestScriptableObjectHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestExtensions.Test1));
            Assert.IsNotNull(ub);

            var helper = UnityEngine.ScriptableObject.CreateInstance<UserlandBindingsTestScriptableObjectHelper>();
            helper.x = 0;

            IStack st = new Stack();
            LogAssert.Expect(LogType.Log, $"Test1 works.");
            if (ub != null)
            {
                ub.Func(helper, st);
            }
            Assert.IsTrue(helper.x == 7);
        }

        [Test]
        public void BindingsWork_CALL_EXTENSION_void_int()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestScriptableObjectHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestExtensions.Test2));
            Assert.IsNotNull(ub);

            var helper = UnityEngine.ScriptableObject.CreateInstance<UserlandBindingsTestScriptableObjectHelper>();
            helper.x = 0;

            int arg1 = 1337;
            IStack st = new Stack();
            st.PushInt(arg1);
            LogAssert.Expect(LogType.Log, $"Test2 works: {arg1}.");
            if (ub != null)
            {
                ub.Func(helper, st);
            }
            Assert.IsTrue(helper.x == arg1);
        }

        [Test]
        public void BindingsWork_CALL_GET_PROPERTY_int()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var name = "get_" + nameof(UserlandBindingsTestHelper.ValueInt);
            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == name);
            Assert.IsNotNull(ub);

            var arg1 = 7;
            var instance = new UserlandBindingsTestHelper();
            instance.propertyValueInt = arg1;

            IStack st = new Stack();
            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.ValueInt)} get works.");
            if (ub != null)
            {
                ub.Func(instance, st);
            }
            var ret = st.PopInt();
            Assert.IsTrue(ret == arg1);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void BindingsWork_CALL_SET_PROPERTY_int()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var name = "set_" + nameof(UserlandBindingsTestHelper.ValueInt);
            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == name);
            Assert.IsNotNull(ub);

            var arg1 = 7;
            var instance = new UserlandBindingsTestHelper();
            //instance.propertyValueInt = arg1;

            IStack st = new Stack();
            st.PushInt(arg1);
            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.ValueInt)} set works.");
            if (ub != null)
            {
                ub.Func(instance, st);
            }
            Assert.IsTrue(instance.propertyValueInt == arg1);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void BindingsWork_CALL_GET_PROPERTY_static_int()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var name = "get_" + nameof(UserlandBindingsTestHelper.StaticValueInt);
            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == name);
            Assert.IsNotNull(ub);

            IStack st = new Stack();
            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.StaticValueInt)} get works.");
            if (ub != null)
            {
                ub.Func(null, st);
            }
            var ret = st.PopInt();
            Assert.IsTrue(ret == 1);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void BindingsWork_CALL_Test13_void_Color()
        {
            var ubs = UserlandBindings.Get(typeof(UserlandBindingsTestHelper));
            Assert.IsNotNull(ubs);

            var ub = (UserlandMethodBinding)ubs.Single(x => x.Name == nameof(UserlandBindingsTestHelper.Test13_void_out_Color));
            Assert.IsNotNull(ub);
            if (ub == null)
                return;

            IStack st = new Stack();
            LogAssert.Expect(LogType.Log, $"{nameof(UserlandBindingsTestHelper.Test13_void_out_Color)} works.");
            var instance = new UserlandBindingsTestHelper();
            ub.Func(instance, st);
            var ret = st.PopColor();
            Assert.IsTrue(ret == Color.red);
        }
    }
}
