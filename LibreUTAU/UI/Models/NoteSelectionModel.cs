using System.Collections.Generic;
using System.Linq;
using LibreUtau.Core.USTx;

namespace LibreUtau.UI.Models {
    public struct SelectionConstraints {
        public int TopNoteNum, BottomNoteNum, EarliestNoteStart, LatestNoteEnd, ShortestNoteDur;
    }

    public class NoteSelection {
        readonly List<UNote> Notes = new List<UNote>();
        private UNote TopNote, BottomNote, EarliestNote, LatestNote, ShortestNote;

        public NoteSelection() { }

        public NoteSelection(UNote note) {
            Notes = new List<UNote> {note};
            TopNote = BottomNote = EarliestNote = LatestNote = ShortestNote = note;
        }

        public NoteSelection(IEnumerable<UNote> notes) {
            Clear();
            foreach (var note in notes) {
                AddNote(note);
            }
        }

        public void AddNote(UNote note) {
            Notes.Add(note);
            if (EarliestNote == null || note.PosTick < EarliestNote.PosTick) EarliestNote = note;
            if (LatestNote == null || note.EndTick > LatestNote.EndTick) LatestNote = note;
            if (BottomNote == null || note.NoteNum < BottomNote.NoteNum) BottomNote = note;
            if (TopNote == null || note.NoteNum > TopNote.NoteNum) TopNote = note;
            if (ShortestNote == null || note.DurTick < ShortestNote.DurTick) ShortestNote = note;
        }

        public void RemoveNote(UNote note) {
            Notes.Remove(note);
            EarliestNote = Notes.Aggregate((note1, note2) => note1.PosTick < note2.PosTick ? note1 : note2);
            LatestNote = Notes.Aggregate((note1, note2) => note1.EndTick > note2.EndTick ? note1 : note2);
            BottomNote = Notes.Aggregate((note1, note2) => note1.NoteNum < note2.NoteNum ? note1 : note2);
            TopNote = Notes.Aggregate((note1, note2) => note1.NoteNum > note2.NoteNum ? note1 : note2);
            ShortestNote = Notes.Aggregate((note1, note2) => note1.DurTick < note2.DurTick ? note1 : note2);
        }

        public List<UNote> GetNotes() => Notes;

        public bool Contains(UNote note) => Notes.Contains(note);

        public int Count => Notes.Count;
        
        public SelectionConstraints GetSelectionConstraints() => new SelectionConstraints() {
            TopNoteNum = TopNote.NoteNum,
            BottomNoteNum = BottomNote.NoteNum,
            EarliestNoteStart = EarliestNote.PosTick,
            LatestNoteEnd = LatestNote.EndTick,
            ShortestNoteDur = ShortestNote.DurTick
        };

        public void Clear() {
            Notes.Clear();
            TopNote = BottomNote = EarliestNote = LatestNote = ShortestNote = null;
        }
    }
}
