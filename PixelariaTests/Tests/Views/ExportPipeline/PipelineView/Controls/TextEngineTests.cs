﻿/*
    Pixelaria
    Copyright (C) 2013 Luiz Fernando Silva

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

    The full license may be found on the License.txt file attached to the
    base directory of this project.
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Pixelaria.Views.ExportPipeline.PipelineView;
using Pixelaria.Views.ExportPipeline.PipelineView.Controls;
using Rhino.Mocks;

namespace PixelariaTests.Tests.Views.ExportPipeline.PipelineView.Controls
{
    [TestClass]
    public class TextEngineTests
    {
        [TestMethod]
        public void TestStartState()
        {
            var buffer = new TextBuffer("Test");
            var sut = new TextEngine(buffer);

            Assert.AreEqual(new Caret(0), sut.Caret, "Should start with caret at beginning of text");
            Assert.AreEqual(buffer, sut.TextBuffer, "Should properly assign passed in text buffer");
        }

        #region Move

        [TestMethod]
        public void TestMoveRight()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.MoveRight();

            Assert.AreEqual(new Caret(1), sut.Caret);
        }

        [TestMethod]
        public void TestMoveRightStopsAtEndOfText()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.MoveRight();
            sut.MoveRight();
            sut.MoveRight();
            sut.MoveRight(); // Should not move right any further

            Assert.AreEqual(new Caret(3), sut.Caret);
        }

        [TestMethod]
        public void TestMoveRightWithSelectionAtEnd()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(0, 2), CaretPosition.End));
            sut.MoveRight();

            Assert.AreEqual(new Caret(3), sut.Caret);
        }

        [TestMethod]
        public void TestMoveRightWithSelectionAtStart()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(0, 2), CaretPosition.Start));
            sut.MoveRight();

            Assert.AreEqual(new Caret(1), sut.Caret);
        }

        [TestMethod]
        public void TestMoveLeft()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(3);

            sut.MoveLeft();

            Assert.AreEqual(new Caret(2), sut.Caret);
        }

        [TestMethod]
        public void TestMoveLeftStopsAtBeginningOfText()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(3);

            sut.MoveLeft();
            sut.MoveLeft();
            sut.MoveLeft();
            sut.MoveLeft(); // Should not move right any further

            Assert.AreEqual(new Caret(0), sut.Caret);
        }

        [TestMethod]
        public void TestMoveLeftWithSelectionAtEnd()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(1, 2), CaretPosition.End));
            sut.MoveLeft();

            Assert.AreEqual(new Caret(2), sut.Caret);
        }

        [TestMethod]
        public void TestMoveLeftWithSelectionAtStart()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(1, 2), CaretPosition.Start));
            sut.MoveLeft();

            Assert.AreEqual(new Caret(0), sut.Caret);
        }

        [TestMethod]
        public void TestMoveToEnd()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.MoveToEnd();

            Assert.AreEqual(new Caret(3), sut.Caret);
        }

        [TestMethod]
        public void TestMoveToEndIdempotent()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.MoveToEnd();
            sut.MoveToEnd();

            Assert.AreEqual(new Caret(3), sut.Caret);
        }

        [TestMethod]
        public void TestMoveToStart()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(3);

            sut.MoveToStart();

            Assert.AreEqual(new Caret(0), sut.Caret);
        }

        [TestMethod]
        public void TestMoveToStartIdempotent()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(3);

            sut.MoveToStart();
            sut.MoveToStart();

            Assert.AreEqual(new Caret(0), sut.Caret);
        }

        #endregion

        #region Selection Move

        [TestMethod]
        public void TestSelectRight()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SelectRight();

            Assert.AreEqual(new Caret(new TextRange(0, 1), CaretPosition.End), sut.Caret);
        }

        [TestMethod]
        public void TestSelectRightStopsAtEndOfText()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SelectRight();
            sut.SelectRight();
            sut.SelectRight();
            sut.SelectRight(); // Should not move right any further

            Assert.AreEqual(new Caret(new TextRange(0, 3), CaretPosition.End), sut.Caret);
        }

        [TestMethod]
        public void TestSelectRightWithSelection()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(0, 2), CaretPosition.Start));

            sut.SelectRight();

            Assert.AreEqual(new Caret(new TextRange(1, 1), CaretPosition.Start), sut.Caret);
        }

        [TestMethod]
        public void TestSelectLeft()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(3);

            sut.SelectLeft();

            Assert.AreEqual(new Caret(new TextRange(2, 1), CaretPosition.Start), sut.Caret);
        }

        [TestMethod]
        public void TestSelectLeftStopsAtBeginningOfText()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(3);

            sut.SelectLeft();
            sut.SelectLeft();
            sut.SelectLeft();
            sut.SelectLeft(); // Should not move left any further

            Assert.AreEqual(new Caret(new TextRange(0, 3), CaretPosition.Start), sut.Caret);
        }

        [TestMethod]
        public void TestSelectToEnd()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SelectToEnd();

            Assert.AreEqual(new Caret(new TextRange(0, 3), CaretPosition.End), sut.Caret);
        }

        [TestMethod]
        public void TestSelectToEndIdempotent()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SelectToEnd();
            sut.SelectToEnd();

            Assert.AreEqual(new Caret(new TextRange(0, 3), CaretPosition.End), sut.Caret);
        }

        [TestMethod]
        public void TestSelectToEndWithSelectionAtStart()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(0, 2), CaretPosition.Start));

            sut.SelectToEnd();

            Assert.AreEqual(new Caret(new TextRange(2, 1), CaretPosition.End), sut.Caret);
        }

        [TestMethod]
        public void TestSelectToStart()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(3);

            sut.SelectToStart();
            
            Assert.AreEqual(new Caret(new TextRange(0, 3), CaretPosition.Start), sut.Caret);
        }

        [TestMethod]
        public void TestSelectToStartIdempotent()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(3);

            sut.SelectToStart();
            sut.SelectToStart();

            Assert.AreEqual(new Caret(new TextRange(0, 3), CaretPosition.Start), sut.Caret);
        }

        [TestMethod]
        public void TestSelectToStartWithSelectionAtEnd()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(1, 2), CaretPosition.End));

            sut.SelectToStart();

            Assert.AreEqual(new Caret(new TextRange(0, 1), CaretPosition.Start), sut.Caret);
        }

        [TestMethod]
        public void TestMoveCaretSelecting()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(1);

            sut.MoveCaretSelecting(2);

            Assert.AreEqual(new Caret(new TextRange(1, 1), CaretPosition.End), sut.Caret);
        }

        [TestMethod]
        public void TestMoveCaretSelectingLeft()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(2);

            sut.MoveCaretSelecting(1);

            Assert.AreEqual(new Caret(new TextRange(1, 1), CaretPosition.Start), sut.Caret);
        }

        [TestMethod]
        public void TestMoveCaretSelectingSamePosition()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(1);

            sut.MoveCaretSelecting(1);

            Assert.AreEqual(new Caret(new TextRange(1, 0), CaretPosition.Start), sut.Caret);
        }

        [TestMethod]
        public void TestMoveCaretSelectingSamePositionStart()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(1, 1), CaretPosition.Start));

            sut.MoveCaretSelecting(3);

            Assert.AreEqual(new Caret(new TextRange(2, 1), CaretPosition.End), sut.Caret);
        }

        [TestMethod]
        public void TestMoveCaretSelectingSamePositionEnd()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(1, 2), CaretPosition.End));

            sut.MoveCaretSelecting(0);

            Assert.AreEqual(new Caret(new TextRange(0, 1), CaretPosition.Start), sut.Caret);
        }

        #endregion

        #region Move Word

        [TestMethod]
        public void TestMoveRightWordEndOfWord()
        {
            var buffer = new TextBuffer("Abc Def");
            var sut = new TextEngine(buffer);

            sut.MoveRightWord();

            Assert.AreEqual(new Caret(3), sut.Caret);
        }

        [TestMethod]
        public void TestMoveRightWordBeginningOfNextWord()
        {
            var buffer = new TextBuffer("Abc   Def");
            var sut = new TextEngine(buffer);

            sut.SetCaret(3);

            sut.MoveRightWord();

            Assert.AreEqual(new Caret(6), sut.Caret);
        }

        [TestMethod]
        public void TestMoveLeftWordBeginningOfWord()
        {
            var buffer = new TextBuffer("Abc Def");
            var sut = new TextEngine(buffer);

            sut.SetCaret(6);

            sut.MoveLeftWord();

            Assert.AreEqual(new Caret(4), sut.Caret);
        }

        [TestMethod]
        public void TestMoveLeftWordBeginningOfFirstWord()
        {
            var buffer = new TextBuffer("Abc Def");
            var sut = new TextEngine(buffer);

            sut.SetCaret(3);

            sut.MoveLeftWord();

            Assert.AreEqual(new Caret(0), sut.Caret);
        }

        [TestMethod]
        public void TestMoveLeftWordEndOfPreviousWord()
        {
            var buffer = new TextBuffer("Abc   Def");
            var sut = new TextEngine(buffer);

            sut.SetCaret(6);

            sut.MoveLeftWord();

            Assert.AreEqual(new Caret(0), sut.Caret);
        }

        [TestMethod]
        public void TestMoveLeftWordBeginningOfWordCaretAtEnd()
        {
            // Tests moving to the previous word when the caret is currently just after the end
            // of a word

            var buffer = new TextBuffer("Abc def ghi");
            var sut = new TextEngine(buffer);

            sut.SetCaret(7);

            sut.MoveLeftWord();

            Assert.AreEqual(new Caret(new TextRange(4, 0), CaretPosition.Start), sut.Caret);
        }

        #endregion

        #region Selection Move Word

        [TestMethod]
        public void TestSelectRightWordEndOfWord()
        {
            var buffer = new TextBuffer("Abc Def");
            var sut = new TextEngine(buffer);

            sut.SelectRightWord();

            Assert.AreEqual(new Caret(new TextRange(0, 3), CaretPosition.End), sut.Caret);
        }

        [TestMethod]
        public void TestSelectRightWordBeginningOfNextWord()
        {
            var buffer = new TextBuffer("Abc   Def");
            var sut = new TextEngine(buffer);

            sut.SetCaret(3);

            sut.SelectRightWord();

            Assert.AreEqual(new Caret(new TextRange(3, 3), CaretPosition.End), sut.Caret);
        }

        [TestMethod]
        public void TestSelectLeftWordBeginningOfWord()
        {
            var buffer = new TextBuffer("Abc Def");
            var sut = new TextEngine(buffer);

            sut.SetCaret(7);

            sut.SelectLeftWord();

            Assert.AreEqual(new Caret(new TextRange(4, 3), CaretPosition.Start), sut.Caret);
        }

        [TestMethod]
        public void TestSelectLeftWordBeginningOfFirstWord()
        {
            var buffer = new TextBuffer("Abc Def");
            var sut = new TextEngine(buffer);

            sut.SetCaret(3);

            sut.SelectLeftWord();

            Assert.AreEqual(new Caret(new TextRange(0, 3), CaretPosition.Start), sut.Caret);
        }

        [TestMethod]
        public void TestSelectLeftWordBeginningOfPreviousWord()
        {
            var buffer = new TextBuffer("Abc   Def");
            var sut = new TextEngine(buffer);

            sut.SetCaret(6);

            sut.SelectLeftWord();

            Assert.AreEqual(new Caret(new TextRange(0, 6), CaretPosition.Start), sut.Caret);
        }

        [TestMethod]
        public void TestSelectLeftWordBeginningOfWordCaretAtEnd()
        {
            // Tests selecting the previous word when the caret is currently just after the end
            // of a word

            var buffer = new TextBuffer("Abc def ghi");
            var sut = new TextEngine(buffer);

            sut.SetCaret(7);

            sut.SelectLeftWord();

            Assert.AreEqual(new Caret(new TextRange(4, 3), CaretPosition.Start), sut.Caret);
        }

        #endregion

        #region Word Segment In

        [TestMethod]
        public void TestWordSegmentIn()
        {
            var buffer = new TextBuffer("Abc def ghi");
            var sut = new TextEngine(buffer);

            var segment = sut.WordSegmentIn(5);

            Assert.AreEqual(new TextRange(4, 3), segment);
        }

        [TestMethod]
        public void TestWordSegmentInEmptyString()
        {
            var buffer = new TextBuffer("");
            var sut = new TextEngine(buffer);

            var segment = sut.WordSegmentIn(0);

            Assert.AreEqual(new TextRange(0, 0), segment);
        }

        [TestMethod]
        public void TestWordSegmentInAtStartOfWord()
        {
            var buffer = new TextBuffer("Abc def ghi");
            var sut = new TextEngine(buffer);

            var segment = sut.WordSegmentIn(4);

            Assert.AreEqual(new TextRange(4, 3), segment);
        }

        [TestMethod]
        public void TestWordSegmentInAtEndOfWord()
        {
            var buffer = new TextBuffer("Abc def ghi");
            var sut = new TextEngine(buffer);

            var segment = sut.WordSegmentIn(7);

            Assert.AreEqual(new TextRange(4, 3), segment);
        }

        [TestMethod]
        public void TestWordSegmentInOverWhitespace()
        {
            var buffer = new TextBuffer("Abc   ghi");
            var sut = new TextEngine(buffer);

            var segment = sut.WordSegmentIn(4);

            Assert.AreEqual(new TextRange(3, 3), segment);
        }

        [TestMethod]
        public void TestWordSegmentInSingleWordText()
        {
            var buffer = new TextBuffer("Abcdef");
            var sut = new TextEngine(buffer);

            var segment = sut.WordSegmentIn(3);

            Assert.AreEqual(new TextRange(0, 6), segment);
        }

        [TestMethod]
        public void TestWordSegmentInSingleWhitespaceText()
        {
            var buffer = new TextBuffer("      ");
            var sut = new TextEngine(buffer);

            var segment = sut.WordSegmentIn(3);

            Assert.AreEqual(new TextRange(0, 6), segment);
        }

        [TestMethod]
        public void TestWordSegmentInBeginningOfString()
        {
            var buffer = new TextBuffer("Abcdef");
            var sut = new TextEngine(buffer);

            var segment = sut.WordSegmentIn(0);

            Assert.AreEqual(new TextRange(0, 6), segment);
        }

        #endregion

        #region Set Caret

        [TestMethod]
        public void TestSetCaret()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(1, 2), CaretPosition.End));

            Assert.AreEqual(new Caret(new TextRange(1, 2), CaretPosition.End), sut.Caret);
        }

        [TestMethod]
        public void TestSetCaretTextRange()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new TextRange(1, 2));

            Assert.AreEqual(new Caret(new TextRange(1, 2), CaretPosition.Start), sut.Caret);
        }

        [TestMethod]
        public void TestSetCaretOffset()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);
            
            sut.SetCaret(new TextRange(2, 2));

            sut.SetCaret(1);

            Assert.AreEqual(new Caret(1), sut.Caret);
        }

        [TestMethod]
        public void TestSetCaretOutOfBoundsStart()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(-5, 0), CaretPosition.Start));

            // Cap at start
            Assert.AreEqual(new Caret(0), sut.Caret);
        }

        [TestMethod]
        public void TestSetCaretOutOfBoundsEnd()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(10, 5), CaretPosition.Start));

            // Cap at end
            Assert.AreEqual(new Caret(3), sut.Caret);
        }

        [TestMethod]
        public void TestSetCaretOutOfBounds()
        {
            var buffer = new TextBuffer("123");
            var sut = new TextEngine(buffer);

            sut.SetCaret(new Caret(new TextRange(-5, 10), CaretPosition.Start));

            // Cap at whole available range
            Assert.AreEqual(new Caret(new TextRange(0, 3), CaretPosition.Start), sut.Caret);
        }

        #endregion

        #region Insert Text

        [TestMethod]
        public void TestInsertTextCaretAtEnd()
        {
            var stub = MockRepository.GenerateStrictMock<ITextEngineTextualBuffer>();

            int length = 0;

            stub.Stub(b => b.TextLength)
                .WhenCalled(inv =>
                {
                    inv.ReturnValue = length;
                })
                .Return(0)
                .TentativeReturn();

            stub.Expect(b => b.Append("456")).WhenCalled(_ => length = 3);
            
            var sut = new TextEngine(stub);
            
            sut.InsertText("456");

            stub.VerifyAllExpectations();
            Assert.AreEqual(new Caret(3), sut.Caret);
        }

        [TestMethod]
        public void TestInsertTextCaretNotAtEnd()
        {
            var stub = MockRepository.GenerateStrictMock<ITextEngineTextualBuffer>();

            stub.Stub(b => b.TextLength).Return(3);
            stub.Expect(b => b.Insert(0, "456"));

            var sut = new TextEngine(stub);

            sut.SetCaret(0);
            sut.InsertText("456");
            
            stub.VerifyAllExpectations();
            Assert.AreEqual(new Caret(3), sut.Caret);
        }

        [TestMethod]
        public void TestInsertTextWithSelection()
        {
            var stub = MockRepository.GenerateStrictMock<ITextEngineTextualBuffer>();

            int length = 3;

            stub.Stub(b => b.TextLength)
                .WhenCalled(inv =>
                {
                    inv.ReturnValue = length;
                })
                .Return(0)
                .TentativeReturn();

            stub.Expect(b => b.Replace(1, 2, "456")).WhenCalled(_ => length = 5);
            
            var sut = new TextEngine(stub);

            sut.SetCaret(new TextRange(1, 2));

            sut.InsertText("456");
            
            stub.VerifyAllExpectations();
            Assert.AreEqual(new Caret(4), sut.Caret);
        }

        #endregion

        #region Backspace

        [TestMethod]
        public void TestBackspace()
        {
            var stub = MockRepository.GenerateStub<ITextEngineTextualBuffer>();

            stub.Stub(b => b.TextLength).Return(3);
            stub.Expect(b => b.Delete(2, 1));

            var sut = new TextEngine(stub);
            
            sut.SetCaret(3);

            sut.BackspaceText();

            stub.VerifyAllExpectations();
            Assert.AreEqual(new Caret(2), sut.Caret);
        }

        [TestMethod]
        public void TestBackspaceAtBeginningHasNoEffect()
        {
            var stub = MockRepository.GenerateStrictMock<ITextEngineTextualBuffer>();
            
            var sut = new TextEngine(stub);

            sut.BackspaceText();

            stub.VerifyAllExpectations();
            Assert.AreEqual(new Caret(0), sut.Caret);
        }

        [TestMethod]
        public void TestBackspaceWithRange()
        {
            var stub = MockRepository.GenerateStub<ITextEngineTextualBuffer>();

            stub.Stub(b => b.TextLength).Return(3);
            stub.Expect(b => b.Delete(1, 2));

            var sut = new TextEngine(stub);

            sut.SetCaret(new TextRange(1, 2));

            sut.BackspaceText();

            stub.VerifyAllExpectations();
            Assert.AreEqual(new Caret(1), sut.Caret);
        }

        [TestMethod]
        public void TestBackspaceAtBeginningWithRange()
        {
            var stub = MockRepository.GenerateStub<ITextEngineTextualBuffer>();

            stub.Stub(b => b.TextLength).Return(3);
            stub.Expect(b => b.Delete(0, 3));

            var sut = new TextEngine(stub);

            sut.SetCaret(new TextRange(0, 3));

            sut.BackspaceText();

            stub.VerifyAllExpectations();
            Assert.AreEqual(new Caret(0), sut.Caret);
        }

        #endregion

        #region Delete

        [TestMethod]
        public void TestDelete()
        {
            var stub = MockRepository.GenerateStub<ITextEngineTextualBuffer>();

            stub.Stub(b => b.TextLength).Return(3);
            stub.Expect(b => b.Delete(0, 1));

            var sut = new TextEngine(stub);
            
            sut.DeleteText();

            stub.VerifyAllExpectations();
            Assert.AreEqual(new Caret(0), sut.Caret);
        }

        [TestMethod]
        public void TestDeleteAtEndHasNoEffect()
        {
            var stub = MockRepository.GenerateStrictMock<ITextEngineTextualBuffer>();

            stub.Stub(b => b.TextLength).Return(3);

            var sut = new TextEngine(stub);

            sut.SetCaret(3);

            sut.DeleteText();

            stub.VerifyAllExpectations();
            Assert.AreEqual(new Caret(3), sut.Caret);
        }

        [TestMethod]
        public void TestDeleteWithRange()
        {
            var stub = MockRepository.GenerateStub<ITextEngineTextualBuffer>();

            stub.Stub(b => b.TextLength).Return(3);
            stub.Expect(b => b.Delete(1, 2));

            var sut = new TextEngine(stub);

            sut.SetCaret(new TextRange(1, 2));

            sut.DeleteText();

            stub.VerifyAllExpectations();
            Assert.AreEqual(new Caret(1), sut.Caret);
        }

        [TestMethod]
        public void TestDeleteAtEndWithRange()
        {
            var stub = MockRepository.GenerateStub<ITextEngineTextualBuffer>();

            stub.Stub(b => b.TextLength).Return(3);
            stub.Expect(b => b.Delete(0, 3));

            var sut = new TextEngine(stub);

            sut.SetCaret(new Caret(new TextRange(0, 3), CaretPosition.End));

            sut.DeleteText();

            stub.VerifyAllExpectations();
            Assert.AreEqual(new Caret(0), sut.Caret);
        }

        #endregion

        internal class TextBuffer : ITextEngineTextualBuffer
        {
            public string Text { get; set; }

            public int TextLength => Text.Length;

            public TextBuffer(string value)
            {
                Text = value;
            }

            public string TextInRange(TextRange range)
            {
                return Text.Substring(range.Start, range.Length);
            }

            public char CharacterAtOffset(int offset)
            {
                return Text[offset];
            }

            public void Delete(int index, int length)
            {
                Text = Text.Remove(index, length);
            }

            public void Insert(int index, string text)
            {
                Text = Text.Insert(index, text);
            }

            public void Append(string text)
            {
                Text += text;
            }

            public void Replace(int index, int length, string text)
            {
                Text = Text.Remove(index, length).Insert(index, text);
            }
        }
    }
}
