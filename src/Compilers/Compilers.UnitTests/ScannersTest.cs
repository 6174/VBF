﻿using VBF.Compilers.Scanners;
using RE = VBF.Compilers.Scanners.RegularExpression;
using NUnit.Framework;
using System;
using VBF.Compilers.Scanners.Generator;
using System.IO;

namespace Compilers.UnitTests
{


    /// <summary>
    ///This is a test class for RegularExpressionTest and is intended
    ///to contain all RegularExpressionTest Unit Tests
    ///</summary>
    [TestFixture]
    public class ScannersTest
    {

        [Test]
        public void RegExToDFATest()
        {
            //var RE_IF = RE.Literal("if");
            //var RE_ELSE = RE.Literal("else");
            var RE_ID = RE.Range('a', 'z').Sequence(
                (RE.Range('a', 'z') | RE.Range('0', '9')).Many());
            //var RE_NUM = RE.Range('0', '9').Many1();
            //var RE_ERROR = RE.Range(Char.MinValue, (char)255);
            Lexicon lexicon = new Lexicon();
            var ID = lexicon.DefaultLexer.DefineToken(RE_ID);

            NFAConverter nfaConverter = new NFAConverter();

            DFAModel D_ID = DFAModel.Create(lexicon);

            //verify state 0
            var state0 = D_ID.States[0];

            Assert.AreEqual(36, state0.OutEdges.Count);
            foreach (var edge in state0.OutEdges)
            {
                Assert.AreEqual(0, edge.TargetState.Index);
            }

            //verify initialization state
            var state1 = D_ID.States[1];

            foreach (var edge in state1.OutEdges)
            {
                if (edge.Symbol >= 'a' && edge.Symbol <= 'z')
                {
                    Assert.IsTrue(edge.TargetState.Index > 0);
                }
                else
                {
                    Assert.AreEqual(0, edge.TargetState.Index);
                }
            }

        }

        [Test]
        public void LexerStateToDFATest()
        {
            Lexicon lexicon = new Lexicon();
            LexerState global = lexicon.DefaultLexer;
            LexerState keywords = global.DefineSubState();
            LexerState xml = keywords.DefineSubState();

            var ID = global.DefineToken(RE.Range('a', 'z').Sequence(
                (RE.Range('a', 'z') | RE.Range('0', '9')).Many()));
            var NUM = global.DefineToken(RE.Range('0', '9').Many1());
            var ERROR = global.DefineToken(RE.Range(Char.MinValue, (char)255));

            var IF = keywords.DefineToken(RE.Literal("if"));
            var ELSE = keywords.DefineToken(RE.Literal("else"));

            var XMLNS = xml.DefineToken(RE.Literal("xmlns"));


            DFAModel dfa = DFAModel.Create(lexicon);

            CompressedTransitionTable tc = CompressedTransitionTable.Compress(dfa);

            ScannerInfo si = lexicon.CreateScannerInfo();

            FiniteAutomationEngine engine = new FiniteAutomationEngine(si.TransitionTable, si.CharClassTable);

            engine.InputString("if");

            Assert.AreEqual(ID.Index, si.GetTokenIndex(engine.CurrentState));

            engine.Reset();
            engine.InputString("12345");
            Assert.AreEqual(NUM.Index, si.GetTokenIndex(engine.CurrentState));

            engine.Reset();
            engine.InputString("asdf12dd");
            Assert.AreEqual(ID.Index, si.GetTokenIndex(engine.CurrentState));

            engine.Reset();
            engine.InputString("A");
            Assert.AreEqual(ERROR.Index, si.GetTokenIndex(engine.CurrentState));

            engine.Reset();
            engine.InputString("AAA");
            Assert.IsTrue(engine.IsAtStoppedState);

            engine.Reset();
            engine.InputString("if ");
            Assert.IsTrue(engine.IsAtStoppedState);

            engine.Reset();
            si.LexerStateIndex = keywords.Index;
            engine.InputString("if");
            Assert.AreEqual(IF.Index, si.GetTokenIndex(engine.CurrentState));

            engine.Reset();
            engine.InputString("else");
            Assert.AreEqual(ELSE.Index, si.GetTokenIndex(engine.CurrentState));

            engine.Reset();
            engine.InputString("xmlns");
            Assert.AreEqual(ID.Index, si.GetTokenIndex(engine.CurrentState));

            engine.Reset();
            si.LexerStateIndex = xml.Index;
            engine.InputString("if");
            Assert.AreEqual(IF.Index, si.GetTokenIndex(engine.CurrentState));

            engine.Reset();
            engine.InputString("xml");
            Assert.IsFalse(engine.IsAtStoppedState);

            engine.Reset();
            engine.InputString("xmlns");
            Assert.AreEqual(XMLNS.Index, si.GetTokenIndex(engine.CurrentState));
            ;
        }

        [Test]
        public void SourceCodeTest()
        {
            string code = @"class ABCDEFG
{
    public int c;
}";
            StringReader sr = new StringReader(code);
            SourceReader source = new SourceReader(sr);

            Assert.AreEqual('c', (char)source.PeekChar());
            Assert.AreEqual('c', (char)source.ReadChar());
            Assert.AreEqual(0, source.Location.CharIndex);

            //create a revert point
            var rp1 = source.CreateRevertPoint();

            Assert.AreEqual('l', (char)source.PeekChar());
            Assert.AreEqual('l', (char)source.ReadChar());
            Assert.AreEqual('a', (char)source.ReadChar());
            Assert.AreEqual(2, source.Location.CharIndex);

            //revert
            source.Revert(rp1);
            Assert.AreEqual('l', (char)source.PeekChar());
            Assert.AreEqual('l', (char)source.ReadChar());
            Assert.AreEqual('a', (char)source.ReadChar());
            Assert.AreEqual(2, source.Location.CharIndex);
            Assert.AreEqual('s', (char)source.ReadChar());
            Assert.AreEqual(3, source.Location.CharIndex);

            source.Revert(rp1);
            source.RemoveRevertPoint(rp1);
            Assert.AreEqual('l', (char)source.ReadChar());
            Assert.AreEqual('a', (char)source.ReadChar());
            Assert.AreEqual(2, source.Location.CharIndex);
            Assert.AreEqual('s', (char)source.ReadChar());
            Assert.AreEqual(3, source.Location.CharIndex);
            Assert.AreEqual('s', (char)source.ReadChar());

            Assert.Catch<ArgumentException>(() => source.Revert(rp1));

            //peek and then revert
            Assert.AreEqual(' ', (char)source.PeekChar());
            var rp2 = source.CreateRevertPoint();
            Assert.AreEqual(' ', (char)source.PeekChar());
            Assert.AreEqual(' ', (char)source.ReadChar());
            Assert.AreEqual('A', (char)source.ReadChar());

            source.Revert(rp2);
            Assert.AreEqual(' ', (char)source.PeekChar());
            Assert.AreEqual(' ', (char)source.ReadChar());
            Assert.AreEqual('A', (char)source.ReadChar());

            //multiple revert point
            var rp3 = source.CreateRevertPoint();
            Assert.AreEqual('B', (char)source.ReadChar());
            Assert.AreEqual('C', (char)source.ReadChar());
            Assert.AreEqual('D', (char)source.ReadChar());
            Assert.AreEqual('E', (char)source.PeekChar());

            source.Revert(rp2);
            Assert.AreEqual(' ', (char)source.PeekChar());
            Assert.AreEqual(' ', (char)source.ReadChar());
            Assert.AreEqual('A', (char)source.ReadChar());

            source.Revert(rp3);
            Assert.AreEqual('B', (char)source.ReadChar());
            Assert.AreEqual('C', (char)source.ReadChar());
            Assert.AreEqual('D', (char)source.ReadChar());
            Assert.AreEqual('E', (char)source.PeekChar());

            source.Revert(rp2);
            Assert.AreEqual(' ', (char)source.PeekChar());
            Assert.AreEqual(' ', (char)source.ReadChar());
            Assert.AreEqual('A', (char)source.ReadChar());

            source.RemoveRevertPoint(rp2);
            source.RemoveRevertPoint(rp3);

            Assert.AreEqual('B', (char)source.ReadChar());
            Assert.AreEqual('C', (char)source.ReadChar());
            Assert.AreEqual('D', (char)source.ReadChar());
            Assert.AreEqual('E', (char)source.ReadChar());
            Assert.AreEqual('F', (char)source.PeekChar());
        }

        [Test]
        public void CacheQueueTest()
        {
            CacheQueue<int> q = new CacheQueue<int>();

            q.Enqueue(1);
            q.Enqueue(2);
            q.Enqueue(3);
            q.Enqueue(4);
            q.Enqueue(5);

            Assert.AreEqual(1, q.Dequeue());
            Assert.AreEqual(2, q.Dequeue());

            Assert.AreEqual(3, q.Count);

            q.Enqueue(6);
            q.Enqueue(7);
            q.Enqueue(8);
            q.Enqueue(9);

            Assert.AreEqual(7, q.Count);

            Assert.AreEqual(3, q[0]);
            Assert.AreEqual(4, q[1]);
            Assert.AreEqual(8, q[5]);
            Assert.AreEqual(9, q[6]);

            Assert.AreEqual(3, q.Dequeue());

            Assert.AreEqual(4, q[0]);
            Assert.AreEqual(9, q[5]);
            Assert.AreEqual(6, q.Count);

            q.Enqueue(10);
            q.Enqueue(11);

            Assert.AreEqual(11, q[7]);
            Assert.AreEqual(8, q.Count);
        }

        [Test]
        public void ScannerTest()
        {
            Lexicon lexicon = new Lexicon();
            LexerState global = lexicon.DefaultLexer;
            LexerState keywords = global.DefineSubState();
            LexerState xml = keywords.DefineSubState();

            var ID = global.DefineToken(RE.Range('a', 'z').Sequence(
                (RE.Range('a', 'z') | RE.Range('0', '9')).Many()));
            var NUM = global.DefineToken(RE.Range('0', '9').Many1());
            var WHITESPACE = global.DefineToken(RE.Symbol(' ').Many());
            var ERROR = global.DefineToken(RE.Range(Char.MinValue, (char)255));

            var IF = keywords.DefineToken(RE.Literal("if"));
            var ELSE = keywords.DefineToken(RE.Literal("else"));

            var XMLNS = xml.DefineToken(RE.Literal("xmlns"));

            ScannerInfo info = lexicon.CreateScannerInfo();
            PeekableScanner scanner = new PeekableScanner(info);

            string source = "asdf04a 1107 else Z if vvv xmlns 772737";
            StringReader sr = new StringReader(source);

            scanner.SetSource(new SourceReader(sr));

            Lexeme l1 = scanner.Read();
            Assert.AreEqual(ID.Index, l1.TokenIndex);
            Assert.AreEqual("asdf04a", l1.Value);
            Assert.AreEqual(0, l1.Span.StartLocation.Column);
            Assert.AreEqual(6, l1.Span.EndLocation.Column);

            Lexeme l2 = scanner.Read();
            Assert.AreEqual(WHITESPACE.Index, l2.TokenIndex);
            Assert.AreEqual(" ", l2.Value);

            Lexeme l3 = scanner.Read();
            Assert.AreEqual(NUM.Index, l3.TokenIndex);
            Assert.AreEqual("1107", l3.Value);

            Lexeme l4 = scanner.Read();
            Assert.AreEqual(WHITESPACE.Index, l4.TokenIndex);

            Lexeme l5 = scanner.Read();
            Assert.AreEqual(ID.Index, l5.TokenIndex);

            int p1 = scanner.Peek();
            Assert.AreEqual(WHITESPACE.Index, p1);

            int p2 = scanner.Peek2();
            Assert.AreEqual(ERROR.Index, p2);

            int p3 = scanner.Peek(3);
            Assert.AreEqual(WHITESPACE.Index, p3);

            int p4 = scanner.Peek(4);
            Assert.AreEqual(ID.Index, p4);

            int p5 = scanner.Peek(5);
            Assert.AreEqual(WHITESPACE.Index, p5);

            Lexeme l6 = scanner.Read();
            Lexeme l7 = scanner.Read();

            Assert.AreEqual(ERROR.Index, l7.TokenIndex);

            int p3_2 = scanner.Peek();
            Assert.AreEqual(p3, p3_2);

            Lexeme l8 = scanner.Read(); // whitespace
            Lexeme l9 = scanner.Read(); // ID:if
            Lexeme l10 = scanner.Read(); // whitespace
            Lexeme l11 = scanner.Read(); // ID:vvv
            Lexeme l12 = scanner.Read(); // whitespace
            Lexeme l13 = scanner.Read(); // ID:xmlns
            Lexeme l14 = scanner.Read(); // whitespace
            Lexeme l15 = scanner.Read(); // NUM:772737
            Lexeme leof = scanner.Read(); // eof

            Assert.AreEqual(info.EndOfStreamTokenIndex, leof.TokenIndex);
            Assert.AreEqual(leof.Span.StartLocation.CharIndex, leof.Span.EndLocation.CharIndex);
            Assert.AreEqual(source.Length, leof.Span.StartLocation.CharIndex);

            Lexeme leof2 = scanner.Read(); //after eof, should return eof again

            Assert.AreEqual(info.EndOfStreamTokenIndex, leof2.TokenIndex);
            Assert.AreEqual(leof.Span.StartLocation.CharIndex, leof2.Span.StartLocation.CharIndex);
        }

        [Test]
        public void SkipTokenTest()
        {
            Lexicon lexicon = new Lexicon();
            LexerState global = lexicon.DefaultLexer;
            LexerState keywords = global.DefineSubState();
            LexerState xml = keywords.DefineSubState();

            var ID = global.DefineToken(RE.Range('a', 'z').Sequence(
                (RE.Range('a', 'z') | RE.Range('0', '9')).Many()));
            var NUM = global.DefineToken(RE.Range('0', '9').Many1());
            var WHITESPACE = global.DefineToken(RE.Symbol(' ').Many());
            var ERROR = global.DefineToken(RE.Range(Char.MinValue, (char)255));

            var IF = keywords.DefineToken(RE.Literal("if"));
            var ELSE = keywords.DefineToken(RE.Literal("else"));

            var XMLNS = xml.DefineToken(RE.Literal("xmlns"));

            ScannerInfo info = lexicon.CreateScannerInfo();
            PeekableScanner scanner = new PeekableScanner(info);

            string source = "asdf04a 1107 else Z if vvv xmlns 772737";
            StringReader sr = new StringReader(source);

            scanner.SetSource(new SourceReader(sr));
            scanner.SetSkipTokens(WHITESPACE.Index);
            info.LexerStateIndex = xml.Index;

            Lexeme l1 = scanner.Read();
            Assert.AreEqual(ID.Index, l1.TokenIndex);
            Assert.AreEqual("asdf04a", l1.Value);

            Lexeme l2 = scanner.Read();
            Assert.AreEqual(NUM.Index, l2.TokenIndex);
            Assert.AreEqual("1107", l2.Value);

            Lexeme l3 = scanner.Read();
            Assert.AreEqual(ELSE.Index, l3.TokenIndex);
            Assert.AreEqual("else", l3.Value);

            Lexeme l4 = scanner.Read();
            Lexeme l5 = scanner.Read();
            Assert.AreEqual(IF.Index, l5.TokenIndex);
            Assert.AreEqual("if", l5.Value);

            int p1 = scanner.Peek();
            Assert.AreEqual(ID.Index, p1);

            int p2 = scanner.Peek2();
            int p3 = scanner.Peek(3);
            int peof = scanner.Peek(4);
            Assert.AreEqual(info.EndOfStreamTokenIndex, peof);

            Lexeme l6 = scanner.Read();
            Lexeme l7 = scanner.Read();
            Assert.AreEqual(XMLNS.Index, l7.TokenIndex);

            Lexeme l8 = scanner.Read();
            Assert.AreEqual(NUM.Index, l8.TokenIndex);

            Lexeme leof = scanner.Read();
            Assert.AreEqual(info.EndOfStreamTokenIndex, leof.TokenIndex);
            Assert.AreEqual(leof.Span.StartLocation.CharIndex, leof.Span.EndLocation.CharIndex);
            Assert.AreEqual(source.Length, leof.Span.StartLocation.CharIndex);
        }
    }
}