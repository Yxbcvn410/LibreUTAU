using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibreUtau.Core.Audio.Build.NAudio;
using LibreUtau.Core.Commands;
using LibreUtau.Core.USTx;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using xxHashSharp;
using static LibreUtau.Core.ResamplerDriver.DriverModels;

namespace LibreUtau.Core.Audio.Render {
    internal class RenderItem {
        public double DurMs;
        public List<ExpPoint> Envelope;

        public int NoteNum;
        public UOto Oto;
        public List<int> PitchData;

        public double PosMs;
        public int RequiredLength;

        // For connector
        public double SkipOver;

        // Sound data
        public MemorySampleProvider Sound;
        // TODO Delay in OffsetAudioProvider must never be negative!

        // For resampler
        public string SourceFile;
        public string StrFlags;
        public double Tempo;
        public int Velocity;
        public int Volume;

        public RenderItem(UPhoneme phoneme, UVoicePart part, UProject project) {
            var singer = project.Tracks[part.TrackNo].Singer;
            // TODO: Check how encoding works here
            SourceFile = Path.Combine(singer.Path, phoneme.Oto.File);

            var strechRatio = Math.Pow(2, 1.0 - (double)(int)phoneme.Parent.Expressions["velocity"].Data / 100);
            var length = phoneme.Oto.Preutter * strechRatio +
                         CommandDispatcher.Inst.Project.TickToMillisecond(phoneme.DurTick) - phoneme.TailIntrude +
                         phoneme.TailOverlap;
            var requiredLength = Math.Ceiling(length / 50 + 1) * 50;
            var lengthAdjustment = phoneme.TailIntrude == 0
                ? phoneme.PreUtter
                : phoneme.PreUtter - phoneme.TailIntrude + phoneme.TailOverlap;

            NoteNum = phoneme.Parent.NoteNum;
            Velocity = (int)phoneme.Parent.Expressions["velocity"].Data;
            Volume = (int)phoneme.Parent.Expressions["volume"].Data;
            StrFlags = phoneme.Parent.GetResamplerFlags();
            PitchData = BuildPitchData(phoneme, part, project);
            RequiredLength = (int)requiredLength;
            Oto = phoneme.Oto;
            Tempo = project.BPM;

            SkipOver = phoneme.Oto.Preutter * strechRatio - phoneme.PreUtter;
            PosMs = project.TickToMillisecond(phoneme.Parent.PosTick + part.PosTick) -
                    phoneme.PreUtter;
            DurMs = project.TickToMillisecond(phoneme.DurTick) + lengthAdjustment;
            Envelope = phoneme.Envelope.Points;
            if (PosMs < 0) {
                SkipOver -= PosMs;
                PosMs = 0;
            }
        }

        public uint HashParameters() {
            return xxHash.CalculateHash(Encoding.UTF8.GetBytes(SourceFile + " " + GetResamplerExeArgs()));
        }

        public string GetResamplerExeArgs() {
            // fresamp.exe <infile> <outfile> <tone> <velocity> <flags> <offset> <length_req>
            // <fixed_length> <endblank> <volume> <modulation> <pitch>
            return
                $"{MusicMath.GetPianoKey(NoteNum)} {Velocity:D} {StrFlags} {Oto.Offset} {RequiredLength:D} {Oto.Consonant} {Oto.Cutoff} {Volume:D} {0:D} {Tempo} {string.Join(",", PitchData)}";
        }

        public ISampleProvider GetSampleProvider() {
            var envelopeSampleProvider = new EnvelopeSampleProvider(Sound, Envelope, SkipOver);
            var sampleRate = Sound.WaveFormat.SampleRate;
            return new OffsetSampleProvider(envelopeSampleProvider) {
                DelayBySamples = (int)(PosMs * sampleRate / 1000),
                TakeSamples = (int)(DurMs * sampleRate / 1000),
                SkipOverSamples = (int)(SkipOver * sampleRate / 1000)
            };
        }

        public EngineInput ToEngineInput() {
            return new EngineInput {
                inputWaveFile = SourceFile,
                NoteString = MusicMath.GetPianoKey(NoteNum).ToString(),
                Velocity = Velocity,
                StrFlags = StrFlags,
                Offset = Oto.Offset,
                RequiredLength = RequiredLength,
                Consonant = Oto.Consonant,
                Cutoff = Oto.Cutoff,
                Volume = Volume,
                Modulation = 0,
                pitchBend = PitchData.ToArray(),
                nPitchBend = PitchData.Count,
                Tempo = Tempo
            };
        }

        private List<int> BuildPitchData(UPhoneme phoneme, UVoicePart part, UProject project) {
            var pitches = new List<int>();
            var lastNote = part.Notes.OrderByDescending(x => x).Where(x => x.CompareTo(phoneme.Parent) < 0)
                .FirstOrDefault();
            var nextNote = part.Notes.Where(x => x.CompareTo(phoneme.Parent) > 0).FirstOrDefault();
            // Get relevant pitch points
            var pps = new List<PitchPoint>();

            var lastNoteInvolved = lastNote != null && phoneme.Overlapped;
            var nextNoteInvolved = nextNote != null && nextNote.Phoneme.Overlapped;

            double lastVibratoStartMs = 0;
            double lastVibratoEndMs = 0;
            double vibratoStartMs = 0;
            double vibratoEndMs = 0;

            if (lastNoteInvolved) {
                var offsetMs =
                    CommandDispatcher.Inst.Project.TickToMillisecond(phoneme.Parent.PosTick - lastNote.PosTick);
                foreach (var pp in lastNote.PitchBend.Points) {
                    var newpp = pp.Clone();
                    newpp.X -= offsetMs;
                    newpp.Y -= (phoneme.Parent.NoteNum - lastNote.NoteNum) * 10;
                    pps.Add(newpp);
                }

                if (lastNote.Vibrato.Depth != 0) {
                    lastVibratoStartMs = -CommandDispatcher.Inst.Project.TickToMillisecond(lastNote.DurTick) *
                        lastNote.Vibrato.Length / 100;
                    lastVibratoEndMs = 0;
                }
            }

            foreach (var pp in phoneme.Parent.PitchBend.Points) {
                pps.Add(pp);
            }

            if (phoneme.Parent.Vibrato.Depth != 0) {
                vibratoEndMs = CommandDispatcher.Inst.Project.TickToMillisecond(phoneme.Parent.DurTick);
                vibratoStartMs = vibratoEndMs * (1 - phoneme.Parent.Vibrato.Length / 100);
            }

            if (nextNoteInvolved) {
                var offsetMs =
                    CommandDispatcher.Inst.Project.TickToMillisecond(phoneme.Parent.PosTick - nextNote.PosTick);
                foreach (var pp in nextNote.PitchBend.Points) {
                    var newpp = pp.Clone();
                    newpp.X -= offsetMs;
                    newpp.Y -= (phoneme.Parent.NoteNum - nextNote.NoteNum) * 10;
                    pps.Add(newpp);
                }
            }

            var startMs = -phoneme.Oto.Preutter;
            var endMs = CommandDispatcher.Inst.Project.TickToMillisecond(phoneme.DurTick) -
                        (nextNote != null && nextNote.Phoneme.Overlapped
                            ? nextNote.Phoneme.PreUtter - nextNote.Phoneme.Overlap
                            : 0);
            if (pps.Count > 0) {
                if (pps.First().X > startMs) {
                    pps.Insert(0, new PitchPoint(startMs, pps.First().Y));
                }

                if (pps.Last().X < endMs) {
                    pps.Add(new PitchPoint(endMs, pps.Last().Y));
                }
            } else {
                throw new Exception("Zero pitch points.");
            }

            // Interpolation
            const int intervalTick = 5;
            var intervalMs = CommandDispatcher.Inst.Project.TickToMillisecond(intervalTick);
            var currMs = startMs;
            var i = 0;

            while (currMs < endMs) {
                while (pps[i + 1].X < currMs) {
                    i++;
                }

                var pit = MusicMath.InterpolateShape(pps[i].Point(), pps[i + 1].Point(), currMs, pps[i].Shape);
                pit *= 10;

                // Apply vibratos
                if (currMs < lastVibratoEndMs && currMs >= lastVibratoStartMs) {
                    pit += InterpolateVibrato(lastNote.Vibrato, currMs - lastVibratoStartMs);
                }

                if (currMs < vibratoEndMs && currMs >= vibratoStartMs) {
                    pit += InterpolateVibrato(phoneme.Parent.Vibrato, currMs - vibratoStartMs);
                }

                pitches.Add((int)pit);
                currMs += intervalMs;
            }

            return pitches;
        }

        private double InterpolateVibrato(VibratoExpression vibrato, double posMs) {
            var lengthMs = vibrato.Length / 100 *
                           CommandDispatcher.Inst.Project.TickToMillisecond(vibrato.Parent.DurTick);
            var inMs = lengthMs * vibrato.In / 100;
            var outMs = lengthMs * vibrato.Out / 100;

            var value = -Math.Sin(2 * Math.PI * (posMs / vibrato.Period + vibrato.Shift / 100)) * vibrato.Depth;

            if (posMs < inMs) {
                value *= posMs / inMs;
            } else if (posMs > lengthMs - outMs) {
                value *= (lengthMs - posMs) / outMs;
            }

            return value;
        }
    }
}
