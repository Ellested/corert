// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

using Internal.Runtime.CompilerServices;

namespace System
{
    public abstract class MulticastDelegate : Delegate, ISerializable
    {
        // This constructor is called from the class generated by the
        //    compiler generated code (This must match the constructor
        //    in Delegate
        protected MulticastDelegate(object target, string method) : base(target, method)
        {
        }

        // This constructor is called from a class to generate a 
        // delegate based upon a static method name and the Type object
        // for the class defining the method.
        protected MulticastDelegate(Type target, string method) : base(target, method)
        {
        }

        private bool InvocationListEquals(MulticastDelegate d)
        {
            Delegate[] invocationList = m_helperObject as Delegate[];
            if (d.m_extraFunctionPointerOrData != m_extraFunctionPointerOrData)
                return false;

            int invocationCount = (int)m_extraFunctionPointerOrData;
            for (int i = 0; i < invocationCount; i++)
            {
                Delegate dd = invocationList[i];
                Delegate[] dInvocationList = d.m_helperObject as Delegate[];
                if (!dd.Equals(dInvocationList[i]))
                    return false;
            }
            return true;
        }

        public override sealed bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (object.ReferenceEquals(this, obj))
                return true;
            if (!InternalEqualTypes(this, obj))
                return false;

            // Since this is a MulticastDelegate and we know
            // the types are the same, obj should also be a
            // MulticastDelegate
            Debug.Assert(obj is MulticastDelegate, "Shouldn't have failed here since we already checked the types are the same!");
            var d = Unsafe.As<MulticastDelegate>(obj);

            // there are 2 kind of delegate kinds for comparision
            // 1- Multicast (m_helperObject is Delegate[])
            // 2- Single-cast delegate, which can be compared with a structural comparision

            if (m_functionPointer == GetThunk(MulticastThunk))
            {
                return InvocationListEquals(d);
            }
            else
            {
                if (!object.ReferenceEquals(m_helperObject, d.m_helperObject) ||
                    (!FunctionPointerOps.Compare(m_extraFunctionPointerOrData, d.m_extraFunctionPointerOrData)) ||
                    (!FunctionPointerOps.Compare(m_functionPointer, d.m_functionPointer)))
                {
                    return false;
                }

                // Those delegate kinds with thunks put themselves into the m_firstParamter, so we can't 
                // blindly compare the m_firstParameter fields for equality.
                if (object.ReferenceEquals(m_firstParameter, this))
                {
                    return object.ReferenceEquals(d.m_firstParameter, d);
                }

                return object.ReferenceEquals(m_firstParameter, d.m_firstParameter);
            }
        }

        public override sealed int GetHashCode()
        {
            Delegate[] invocationList = m_helperObject as Delegate[];
            if (invocationList == null)
            {
                return base.GetHashCode();
            }
            else
            {
                int hash = 0;
                for (int i = 0; i < (int)m_extraFunctionPointerOrData; i++)
                {
                    hash = hash * 33 + invocationList[i].GetHashCode();
                }

                return hash;
            }
        }

        // Force inline as the true/false ternary takes it above ALWAYS_INLINE size even though the asm ends up smaller
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(MulticastDelegate d1, MulticastDelegate d2)
        {
            // Test d2 first to allow branch elimination when inlined for null checks (== null)
            // so it can become a simple test
            if (d2 is null)
            {
                // return true/false not the test result https://github.com/dotnet/runtime/issues/4207
                return (d1 is null) ? true : false;
            }

            return ReferenceEquals(d2, d1) ? true : d2.Equals((object)d1);
        }

        // Force inline as the true/false ternary takes it above ALWAYS_INLINE size even though the asm ends up smaller
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(MulticastDelegate d1, MulticastDelegate d2)
        {
            // Can't call the == operator as it will call object==

            // Test d2 first to allow branch elimination when inlined for not null checks (!= null)
            // so it can become a simple test
            if (d2 is null)
            {
                // return true/false not the test result https://github.com/dotnet/runtime/issues/4207
                return (d1 is null) ? false : true;
            }

            return ReferenceEquals(d2, d1) ? false : !d2.Equals(d1);
        }

        public override sealed Delegate[] GetInvocationList()
        {
            return base.GetInvocationList();
        }

        protected override sealed Delegate CombineImpl(Delegate follow)
        {
            return base.CombineImpl(follow);
        }
        protected override sealed Delegate RemoveImpl(Delegate value)
        {
            return base.RemoveImpl(value);
        }

        protected override MethodInfo GetMethodImpl()
        {
            return base.GetMethodImpl();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException(SR.Serialization_DelegatesNotSupported);
        }
    }
}
