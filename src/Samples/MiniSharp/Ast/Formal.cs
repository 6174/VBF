﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VBF.Compilers.Scanners;

namespace VBF.MiniSharp.Ast
{
    public class Formal : AstNode
    {
        public Type Type { get; private set; }
        public LexemeValue ParameterName { get; private set; }

        public Formal(Type type, LexemeValue paramName)
        {
            Type = type;
            ParameterName = paramName;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitFormal(this);
        }
    }
}
