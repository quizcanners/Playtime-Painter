using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.Utils
{
    public static class QcDebug 
    {
        public static string CaseNotImplemented(object unimplementedValue) 
            => "Case [{0}] for [{1}] is not implemented".F(
                unimplementedValue.ToString().SimplifyTypeName(),
                unimplementedValue.GetType().ToPegiStringType());

        public static string CaseNotImplemented(object unimplementedValue, string context)
           => "Case [{0}] for [{1}] is not implemented for {2}".F(
               unimplementedValue.ToString().SimplifyTypeName(), 
               unimplementedValue.GetType().ToPegiStringType(),
               context
               );


        public static void Inspect() 
        {

        } 

    }
}
