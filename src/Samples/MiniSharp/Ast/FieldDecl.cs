﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VBF.Compilers.Scanners;

namespace VBF.MiniSharp.Ast
{
    public class FieldDecl : AstNode
    {
        public Field FieldInfo { get; set; }
        public Type Type { get; private set; }
        public LexemeValue FieldName { get; private set; }

        public FieldDecl(Type type, LexemeValue fieldName)
        {
            Type = type;
            FieldName = fieldName;
        }

        public override T Accept<T>(IAstVisitor<T> visitor)
        {
            return visitor.VisitFieldDecl(this);
        }
    }
}
