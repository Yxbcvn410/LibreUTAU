using System;
using System.Linq;
using LibreUtau.Core.Audio.Build;
using LibreUtau.Core.USTx;

namespace LibreUtau.Core.Commands {
    public class ProjectWatcher : ICmdSubscriber {
        private UVoicePart Part;

        public ProjectWatcher() {
            CommandDispatcher.Inst.AddSubscriber(this);
            this.Part = null;
        }

        # region ICmdSubscriber

        public void OnCommandExecuted(UCommand cmd, bool isUndo) {
            switch (cmd) {
                case LoadProjectNotification command:
                    OnProjectLoad(command);
                    break;
                case LoadPartNotification command:
                    Part = command.part as UVoicePart;
                    UpdatePart(Part);
                    break;
                case RemovePartCommand command:
                    if (command.Part == Part)
                        Part = null;
                    break;
                case NoteCommand _:
                    UpdatePart(Part);
                    break;
                case ExpCommand _:
                    UpdatePart(Part);
                    break;
                case TrackChangeSingerCommand command:
                    foreach (var part in command.project.Parts.OfType<UVoicePart>()) UpdatePart(part);

                    break;
                case MovePartCommand command:
                    if (command.Part is UVoicePart voicePart &&
                        command.Project.Tracks[command.oldTrackNo].Singer !=
                        command.Project.Tracks[command.newTrackNo].Singer) {
                        UpdatePart(voicePart);
                    }

                    break;
            }
        }

        # endregion

        public void UpdatePart(UVoicePart part) {
            if (part == null) return;
            lock (part) {
                CommandDispatcher.Inst.Project.Built = false; // TODO Сделать нормально
                CheckOverlappedNotes(part);
                UpdatePhonemes(part);
                UpdateEnvelope(part);
                UpdatePitchBend(part);
                CommandDispatcher.Inst.ExecuteCmd(new RedrawNotesNotification(), true);
            }
        }

        private void UpdatePitchBend(UVoicePart part) {
            UNote lastNote = null;
            foreach (UNote note in part.Notes) {
                if (note.PitchBend.SnapFirst) {
                    if (note.Phoneme != null && lastNote != null &&
                        (note.Phoneme.Overlapped || note.PosTick == lastNote.EndTick))
                        note.PitchBend.Points[0].Y = (lastNote.NoteNum - note.NoteNum) * 10;
                    else
                        note.PitchBend.Points[0].Y = 0;
                }

                lastNote = note;
            }
        }

        public void ResnapPitchBend(UVoicePart part) {
            UNote lastNote = null;
            foreach (UNote note in part.Notes) {
                if (!note.PitchBend.SnapFirst) {
                    if (note.Phoneme != null && note.Phoneme.Overlapped && lastNote != null)
                        if (note.PitchBend.Points[0].Y == (lastNote.NoteNum - note.NoteNum) * 10)
                            note.PitchBend.SnapFirst = true;
                }

                lastNote = note;
            }
        }

        private void UpdateEnvelope(UVoicePart part) {
            foreach (UNote note in part.Notes) {
                note.Phoneme.Envelope.Points[0].X = -note.Phoneme.PreUtter;
                note.Phoneme.Envelope.Points[1].X =
                    note.Phoneme.Envelope.Points[0].X + (note.Phoneme.Overlapped ? note.Phoneme.Overlap : 5);
                note.Phoneme.Envelope.Points[2].X = Math.Max(0, note.Phoneme.Envelope.Points[1].X);
                note.Phoneme.Envelope.Points[3].X =
                    CommandDispatcher.Inst.Project.TickToMillisecond(note.Phoneme.DurTick) - note.Phoneme.TailIntrude;
                note.Phoneme.Envelope.Points[4].X = note.Phoneme.Envelope.Points[3].X + note.Phoneme.TailOverlap;

                note.Phoneme.Envelope.Points[1].Y = (int)note.Phoneme.Parent.Expressions["volume"].Data;
                note.Phoneme.Envelope.Points[1].X = note.Phoneme.Envelope.Points[0].X +
                                                    (note.Phoneme.Overlapped ? note.Phoneme.Overlap : 5) *
                                                    (int)note.Phoneme.Parent.Expressions["accent"].Data / 100.0;
                note.Phoneme.Envelope.Points[1].Y = (int)note.Phoneme.Parent.Expressions["accent"].Data *
                    (int)note.Phoneme.Parent.Expressions["volume"].Data / 100.0;
                note.Phoneme.Envelope.Points[2].Y = (int)note.Phoneme.Parent.Expressions["volume"].Data;
                note.Phoneme.Envelope.Points[3].Y = (int)note.Phoneme.Parent.Expressions["volume"].Data;
                note.Phoneme.Envelope.Points[3].X -=
                    (note.Phoneme.Envelope.Points[3].X - note.Phoneme.Envelope.Points[2].X) *
                    (int)note.Phoneme.Parent.Expressions["decay"].Data / 500;
                note.Phoneme.Envelope.Points[3].Y *= 1.0 - (int)note.Phoneme.Parent.Expressions["decay"].Data / 100.0;
            }
        }

        private void UpdatePhonemes(UVoicePart part) {
            var singer = CommandDispatcher.Inst.Project.Tracks[part.TrackNo].Singer;
            if (singer == null || !singer.Loaded) return;
            UNote previousNote = null;
            foreach (UNote note in part.Notes) {
                // Update oto
                note.UpdatePhoneme(singer);

                // Overlap correction
                note.Phoneme.DurTick = note.DurTick;
                if (previousNote != null) {
                    var phoneme = note.Phoneme;
                    var lastPhoneme = previousNote.Phoneme;
                    int gapTick = phoneme.Parent.PosTick - lastPhoneme.Parent.PosTick -
                                  lastPhoneme.DurTick;
                    double gapMs = CommandDispatcher.Inst.Project.TickToMillisecond(gapTick);
                    if (gapMs < phoneme.PreUtter) {
                        phoneme.Overlapped = true;
                        double lastDurMs = CommandDispatcher.Inst.Project.TickToMillisecond(lastPhoneme.DurTick);
                        double correctionRatio =
                            (lastDurMs + Math.Min(0, gapMs)) / 2 / (phoneme.PreUtter - phoneme.Overlap);
                        if (phoneme.PreUtter - phoneme.Overlap > gapMs + lastDurMs / 2) {
                            phoneme.OverlapCorrection = true;
                            phoneme.PreUtter = gapMs + (phoneme.PreUtter - gapMs) * correctionRatio;
                            phoneme.Overlap *= correctionRatio;
                        } else if (phoneme.PreUtter > gapMs + lastDurMs) {
                            phoneme.OverlapCorrection = true;
                            phoneme.Overlap *= correctionRatio;
                            phoneme.PreUtter = gapMs + lastDurMs;
                        } else
                            phoneme.OverlapCorrection = false;

                        lastPhoneme.TailIntrude = phoneme.PreUtter - gapMs;
                        lastPhoneme.TailOverlap = phoneme.Overlap;
                    } else {
                        phoneme.Overlapped = false;
                        lastPhoneme.TailIntrude = 0;
                        lastPhoneme.TailOverlap = 0;
                    }
                }

                // TODO Почистить тут, убрать ненужные поля из Phoneme
                previousNote = note;
            }
        }

        private void CheckOverlappedNotes(UVoicePart part) {
            UNote lastNote = null;
            foreach (UNote note in part.Notes) {
                if (lastNote != null && lastNote.EndTick > note.PosTick) {
                    lastNote.Error = true;
                    note.Error = true;
                } else note.Error = false;

                lastNote = note;
            }
        }

        # region Cmd Handling

        private void OnProjectLoad(UNotification cmd) {
            foreach (UPart part in cmd.project.Parts)
                if (part is UVoicePart)
                    UpdatePart((UVoicePart)part);
            NoteCacheProvider.SetCacheDir(PathManager.Inst.GetCachePath(cmd.project));
            NoteCacheProvider.CleanupCache(true);
        }

        # endregion
    }
}
