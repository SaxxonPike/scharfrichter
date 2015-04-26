using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scharfrichter.Codec.Charts
{
    public class Chart
    {
        private List<Entry> entries = new List<Entry>();
        private Dictionary<int, Fraction> lengths = new Dictionary<int, Fraction>();
        private Dictionary<string, string> tags = new Dictionary<string, string>();

        public Fraction DefaultBPM = new Fraction(0, 1);
        public Fraction TickRate = new Fraction(0, 1);


        // Add judgement entries to the list. It's unsure if these are needed, but
        // it can be used if there are compatibility issues with converted arcade data.
        // IIDX: F0, FA, FF, 03, 08, 12
        // 5key: F4, FC, FF, 03, 06, 0E
        public void AddJudgements()
        {
            int[] judgementValues = new int[] { 0xF0, 0xFA, 0xFF, 0x03, 0x08, 0x12 };
            int judgementCount = judgementValues.Length;
            int playerCount = Players;

            for (int j = 0; j < playerCount; j++)
            {
                for (int i = 0; i < judgementCount; i++)
                {
                    Entry entry = new Entry();
                    entry.Column = 0;
                    entry.LinearOffset = new Fraction(0, 1);
                    entry.MetricMeasure = 0;
                    entry.MetricOffset = new Fraction(0, 1);
                    entry.Parameter = i;
                    entry.Player = j + 1;
                    entry.Type = EntryType.Judgement;
                    entry.Value = new Fraction(judgementValues[i], 1);
                    entries.Add(entry);
                }
            }
        }

        // Add measure line entries to the list. Can only be used when
        // metric data is present.
        public void AddMeasureLines()
        {
            int measureCount = -1;
            int playerCount = Players;

            // verify all required metric info is present
            foreach (Entry entry in entries)
                if (!entry.MetricOffsetInitialized)
                    throw new Exception("Measure lines can't be added because at least one entry is missing Metric offset information.");

            // clear up existing ones
            RemoveMeasureLines();

            // find the highest measure index
            foreach (Entry entry in entries)
            {
                if (entry.MetricMeasure >= measureCount)
                    measureCount = entry.MetricMeasure + 1;
            }

            // add measure lines for each measure
            for (int i = 0; i < measureCount; i++)
            {
                for (int j = 0; j < playerCount; j++)
                {
                    Entry entry = new Entry();
                    entry.Column = 0;
                    entry.MetricMeasure = i;
                    entry.MetricOffset = new Fraction(0, 1);
                    entry.Player = j + 1;
                    entry.Type = EntryType.Measure;
                    entry.Value = new Fraction(0, 1);
                    entries.Add(entry);
                }
            }

            // add end of song marker
            if (measureCount >= 0)
            {
                for (int j = 0; j < playerCount; j++)
                {
                    Entry entry = new Entry();
                    entry.Column = 0;
                    entry.MetricMeasure = measureCount;
                    entry.MetricOffset = new Fraction(0, 1);
                    entry.Player = j + 1;
                    entry.Type = EntryType.EndOfSong;
                    entry.Value = new Fraction(0, 1);
                    entries.Add(entry);
                }
            }
        }

        // Convert Metric offsets to Linear offsets for the entry list.
        public void CalculateLinearOffsets()
        {
            // verify all required metric info is present
            foreach (Entry entry in entries)
                if (!entry.MetricOffsetInitialized)
                    throw new Exception("Linear offsets can't be calculated because at least one entry is missing Metric offset information.");

            // delete all linear offset data
            ClearLinearOffsets();

            // make sure everything is sorted before we begin
            entries.Sort();

            // initialization
            Fraction baseLinear = new Fraction(0, 1);
            Fraction bpm = DefaultBPM;
            Fraction lastMetric = new Fraction(0, 1);
            Fraction length = new Fraction(0, 1);
            int measure = -1;
            Fraction measureRate = new Fraction(0, 1);
            Fraction rate = new Fraction(0, 1);
            
            // BPM into seconds per measure
            measureRate = Util.CalculateMeasureRate(bpm);

            foreach (Entry entry in entries)
            {
                // on measures, update rate information
                if (entry.Type == EntryType.Measure)
                {
                    baseLinear += rate;
                    measure = entry.MetricMeasure;

                    if (lengths.ContainsKey(measure))
                    {
                        length = lengths[measure];
                        rate = length * measureRate;
                    }
                    else
                    {
                        length = new Fraction(1, 1);
                        rate = measureRate;
                    }
                    lastMetric = new Fraction(0, 1);
                }

                // calculate linear offset
                Fraction entryOffset = entry.MetricOffset;
                entryOffset -= lastMetric;
                entryOffset *= rate;
                entryOffset += baseLinear;
                entry.LinearOffset = entryOffset;

                // on tempo change, update rate information
                if (entry.Type == EntryType.Tempo)
                {
                    measureRate = Util.CalculateMeasureRate(bpm);
                    rate = length * measureRate;
                    lastMetric = entry.MetricOffset;
                }
            }
        }

        // Convert Linear offsets into Metric offsets for the entry list.
        public void CalculateMetricOffsets()
        {
            // verify all required linear info is present
            foreach (Entry entry in entries)
            {
                if (!entry.LinearOffsetInitialized)
                    throw new Exception("Metric offsets can't be calculated because at least one entry is missing Linear offset information.");
            }

            // delete all metric offset data
            ClearMetricOffsets();

            // make sure everything is sorted before we begin
            entries.Sort();

            // initialization
            Fraction bpm = DefaultBPM;
            Fraction lastMeasureOffset = new Fraction(0, 1);
            Fraction lastTempoOffset = new Fraction(0, 1);
            Fraction length = new Fraction(0, 1);
            Dictionary<int, Fraction> lengthList = new Dictionary<int, Fraction>();
            int measure = 0;
            Fraction measureLength = new Fraction(0, 1);
            Fraction metricBase = new Fraction(0, 1);
            Fraction rate = Util.CalculateMeasureRate(bpm);
            bool tempoChanged = false;

            // discard length list since it will be generated later
            lengths.Clear();

            // get measure lengths for non-tempo-changing measures
            foreach (Entry entry in entries)
            {
                if (entry.Type == EntryType.Measure || entry.Type == EntryType.EndOfSong)
                {
                    if (entry.LinearOffset != lastMeasureOffset)
                    {
                        if (!tempoChanged)
                        {
                            Fraction distance = entry.LinearOffset - lastMeasureOffset;
                            if ((double)distance < 0)
                                throw new Exception("INTERNAL ERROR DAMMIT.");
                            lengthList.Add(measure, distance);
                        }
                        lastMeasureOffset = entry.LinearOffset;
                        measure++;
                        tempoChanged = false;
                    }
                }
                else if (entry.Type == EntryType.Tempo)
                {
                    if ((entry.LinearOffset - lastMeasureOffset).Numerator != 0)
                    {
                        tempoChanged = true;
                    }
                }
            }

            // initialization for the calculation phase
            measure = 0;
            lastMeasureOffset = new Fraction(0, 1);

            Fraction tickMeasureLength;
            if (lengthList.ContainsKey(0))
                tickMeasureLength = lengthList[0];
            else
                tickMeasureLength = new Fraction(0, 1);

            List<Entry> entryList = new List<Entry>();
            List<Entry> measureEntryList = new List<Entry>();

            // calculate metric offsets
            foreach (Entry entry in entries)
            {
                // on any measure, end of song or tempo events, update the metric rate information
                if (entry.Type == EntryType.Measure || entry.Type == EntryType.Tempo || entry.Type == EntryType.EndOfSong)
                {
                    Fraction measureDistance = ((entry.LinearOffset - lastTempoOffset) * TickRate) / rate;

                    // calculate metric offset for entries in tempo-changing measures
                    foreach (Entry tempoEntry in entryList)
                    {
                        tempoEntry.MetricOffset = Fraction.Shrink(measureLength + (((tempoEntry.LinearOffset - lastTempoOffset) / (entry.LinearOffset - lastTempoOffset)) * measureDistance));
                        tempoEntry.MetricMeasure = measure;
                        measureEntryList.Add(tempoEntry);
                    }

                    measureLength += measureDistance;

                    if (entry.Type == EntryType.Measure || entry.Type == EntryType.EndOfSong)
                    {
                        if (entry.LinearOffset != lastMeasureOffset)
                        {
                            // apply measure length to all entries in a tempo-changing measure
                            // so it's actually using the proper scale
                            foreach (Entry measureEntry in measureEntryList)
                            {
                                Fraction temp = measureEntry.MetricOffset;
                                temp /= measureLength;
                                measureEntry.MetricOffset = Fraction.Shrink(temp);
                                while ((double)measureEntry.MetricOffset >= 1)
                                {
                                    Fraction offs = measureEntry.MetricOffset;
                                    measureEntry.MetricMeasure++;
                                    offs.Numerator -= offs.Denominator;
                                    measureEntry.MetricOffset = offs;
                                }
                            }
                            measureEntryList.Clear();
                            MeasureLengths[measure] = measureLength;
                            measure++;
                            lastMeasureOffset = Fraction.Shrink(entry.LinearOffset);
                            measureLength = new Fraction(0, 1);
                        }
                        entry.MetricOffset = new Fraction(0, 1);
                        entry.MetricMeasure = measure;
                    }
                    else if (entry.Type == EntryType.Tempo)
                    {
                        // on tempo change, update rate
                        bpm = entry.Value;
                        rate = Util.CalculateMeasureRate(bpm);
                    }
                    lastTempoOffset = entry.LinearOffset;

                    // some measures have a defined length, use this on non-tempo-changing measures
                    // for best accuracy
                    if (lengthList.ContainsKey(measure))
                        tickMeasureLength = lengthList[measure];
                    else
                        tickMeasureLength = new Fraction(0, 1);

                    entryList.Clear();
                }

                // calculate metric offset
                if (tickMeasureLength.Numerator > 0)
                {
                    entry.MetricOffset = Fraction.Shrink((entry.LinearOffset - lastTempoOffset) / tickMeasureLength);
                    entry.MetricMeasure = measure;
                    while ((double)entry.MetricOffset >= 1)
                    {
                        Fraction offs = entry.MetricOffset;
                        entry.MetricMeasure++;
                        offs.Numerator -= offs.Denominator;
                        entry.MetricOffset = Fraction.Shrink(offs);
                    }
                }
                else
                {
                    entryList.Add(entry);
                }
            }
        }

        public void ClearLinearOffsets()
        {
            foreach (Entry entry in entries)
            {
                entry.LinearOffset = new Fraction(0, 1);
                entry.LinearOffsetInitialized = false;
            }
        }

        public void ClearMetricOffsets()
        {
            foreach (Entry entry in entries)
            {
                entry.MetricOffset = new Fraction(0, 1);
                entry.MetricMeasure = 0;
                entry.MetricOffsetInitialized = false;
            }
        }

        // clear the Used flag on all entries
        public void ClearUsed()
        {
            foreach (Entry entry in entries)
                entry.Used = false;
        }

        // Entries property
        public List<Entry> Entries
        {
            get
            {
                return entries;
            }
        }

        // Measures property
        public int Measures
        {
            get
            {
                int measureCount = -1;

                // find the highest measure index
                foreach (Entry entry in entries)
                {
                    if (entry.MetricMeasure >= measureCount)
                        measureCount = entry.MetricMeasure + 1;
                }
                return measureCount + 1;
            }
        }

        // MeasureLengths property
        public Dictionary<int, Fraction> MeasureLengths
        {
            get
            {
                return lengths;
            }
        }

        // determine the number of notes a player must play
        public int NoteCount(int player)
        {
            int result = 0;

            foreach (Entry entry in entries)
                if (entry.Type == EntryType.Marker && entry.Player == player)
                    result++;

            return result;
        }

        // determine the number of non-bgm notes
        public int NoteTotal
        {
            get
            {
                int result = 0;

                foreach (Entry entry in entries)
                    if (entry.Type == EntryType.Marker && entry.Player > 0)
                        result++;

                return result;
            }
        }

        // determine the number of players
        public int Players
        {
            get
            {
                int result = 0;
                foreach (Entry entry in entries)
                {
                    if ((entry.Type == EntryType.Marker || entry.Type == EntryType.Sample) && (entry.Player > result))
                        result = entry.Player;
                }
                return result;
            }
        }

        // quantize measure lengths so that they are easier to decipher.
        // many people use BMSE, and BMSE doesn't like measure lengths that are not a multiple of 1/64,
        // so this is here to please the people that still use it. (I hate you guys.)
        public void QuantizeMeasureLengths(int quantizeValue)
        {
            // verify all required metric info is present
            foreach (Entry entry in entries)
                if (!entry.MetricOffsetInitialized)
                    throw new Exception("Measure lengths can't be quantized because at least one entry is missing Metric offset information.");

            double quantizationFloat = quantizeValue;
            int measureCount = Measures;
            Fraction lengthBefore = Fraction.Rationalize(TotalMeasureLength);

            // quantize the measure lengths
            for (int i = 0; i < measureCount; i++)
            {
                if (lengths.ContainsKey(i))
                {
                    if (lengths[i].Denominator != quantizeValue)
                        lengths[i] = new Fraction((long)(Math.Round((double)Fraction.Shrink(lengths[i]) * quantizationFloat)), quantizeValue);
                }
            }

#if (true)
            // since we adjusted measure lengths, we also need to adjust BPMs
            Fraction lengthAfter = Fraction.Rationalize(TotalMeasureLength);
            Fraction ratio = lengthAfter / lengthBefore;

            foreach (Entry entry in entries)
            {
                if (entry.Type == EntryType.Tempo)
                {
                    entry.Value *= ratio;
                }
            }

            DefaultBPM *= ratio;
#endif
            
#if (false) // disabled for now because it is a little buggy and we need to get a release out
            // regenerate linear offsets because the values could have changed
            CalculateLinearOffsets();
#endif
        }

        // quantize Metric note offsets. This is useful for reducing the size of a
        // converted BMS file.
        public void QuantizeNoteOffsets(int quantizeValue)
        {
            // verify all required metric info is present
            foreach (Entry entry in entries)
                if (!entry.MetricOffsetInitialized)
                    throw new Exception("Metric note offsets can't be quantized because at least one entry is missing Metric offset information.");

            int measure = 0;
            Fraction lastMeasure = new Fraction(0, 1);
            long quantize = 0;

            // quantize each event
            foreach (Entry entry in entries)
            {
                if (entry.Type == EntryType.Measure || quantize == 0)
                {
                    if (entry.Type == EntryType.Measure && entry.MetricOffset != lastMeasure)
                        measure++;

                    if (lengths.ContainsKey(measure))
                        quantize = quantizeValue * (long)(Math.Round((double)lengths[measure]));
                    else
                        quantize = quantizeValue;
                }

                if (quantize == 0)
                    quantize = 192;

                if (entry.Type == EntryType.Marker && entry.MetricOffset.Denominator > quantize)
                    entry.MetricOffset = Fraction.Quantize(entry.MetricOffset, quantize);
                else
                    entry.MetricOffset = Fraction.Reduce(entry.MetricOffset);
            }
        }

        // remove all judgement information from the chart.
        public void RemoveJudgements()
        {
            foreach (Entry entry in entries)
            {
                if (entry.Type == EntryType.Judgement)
                    entry.Type = EntryType.Invalid;
            }
        }

        // remove all measure lines from the chart.
        public void RemoveMeasureLines()
        {
            foreach (Entry entry in entries)
            {
                if (entry.Type == EntryType.Measure || entry.Type == EntryType.EndOfSong)
                    entry.Type = EntryType.Invalid;
            }
        }

        // Tags property
        public Dictionary<string, string> Tags
        {
            get
            {
                return tags;
            }
        }

        // get the sum of all measure lengths.
        public double TotalMeasureLength
        {
            get
            {
                double result = 0;
                int measureCount = Measures;
                for (int i = 0; i < measureCount; i++)
                {
                    if (lengths.ContainsKey(i))
                    {
                        result += (double)lengths[i];
                    }
                    else
                    {
                        result += 1;
                    }
                }
                return result;
            }
        }

        // get a list of samples used in the chart. It can be used as a sample map.
        public int[] UsedSamples()
        {
            List<int> result = new List<int>();
            foreach (Entry entry in entries)
            {
                if (entry.Type == EntryType.Marker || entry.Type == EntryType.Sample)
                {
                    int val = (int)((double)entry.Value);
                    if (val > 0)
                    {
                        if (!result.Contains(val))
                        {
                            result.Add(val);
                        }
                    }
                }
            }

            result.Sort();
            return result.ToArray();
        }
    }

    public class Entry : IComparable<Entry>
    {
        private Fraction linearOffset;
        private Fraction metricOffset;
        private Fraction value;

        public bool LinearOffsetInitialized;
        public int MetricMeasure;
        public bool MetricOffsetInitialized;
        public bool ValueInitialized;

        public int Column;
        public bool Freeze;
        public int Parameter;
        public int Player;
        public EntryType Type;
        public bool Used;

        public int CompareTo(Entry other)
        {
            if (other.MetricOffsetInitialized && this.MetricOffsetInitialized)
            {
                if (other.MetricMeasure > this.MetricMeasure)
                    return -1;
                if (other.MetricMeasure < this.MetricMeasure)
                    return 1;

                double myFloat = (double)metricOffset;
                double otherFloat = (double)other.metricOffset;

                if (otherFloat > myFloat)
                    return -1;
                if (otherFloat < myFloat)
                    return 1;
            }
            else if (other.LinearOffsetInitialized && this.LinearOffsetInitialized)
            {
                double myFloat = (double)linearOffset;
                double otherFloat = (double)other.linearOffset;

                if (otherFloat > myFloat)
                    return -1;
                if (otherFloat < myFloat)
                    return 1;
            }

            if (other.Player > this.Player)
                return -1;
            if (other.Player < this.Player)
                return 1;

            if (other.Column > this.Column)
                return -1;
            if (other.Column < this.Column)
                return 1;

            if (other.Parameter > this.Parameter)
                return -1;
            if (other.Parameter < this.Parameter)
                return 1;

            // these must come at the beginning
            if (this.Type == EntryType.Measure && other.Type != EntryType.Measure)
            {
                return -1;
            }
            if (this.Type != EntryType.Measure && other.Type == EntryType.Measure)
            {
                return 1;
            }
            if (this.Type == EntryType.Tempo && other.Type != EntryType.Tempo)
            {
                return -1;
            }
            if (this.Type != EntryType.Tempo && other.Type == EntryType.Tempo)
            {
                return 1;
            }
            if (this.Type == EntryType.Sample && other.Type != EntryType.Sample)
            {
                return -1;
            }
            if (this.Type != EntryType.Sample && other.Type == EntryType.Sample)
            {
                return 1;
            }

            // these must come at the end
            if (this.Type == EntryType.EndOfSong && other.Type != EntryType.EndOfSong)
            {
                return 1;
            }
            if (this.Type != EntryType.EndOfSong && other.Type == EntryType.EndOfSong)
            {
                return -1;
            }

            // at this point, order does not matter
            return 0;
        }

        public override string ToString()
        {
            // for debug purposes only
            return ("[M" + MetricMeasure.ToString() + ":" + metricOffset.ToString() + ", L" + linearOffset.ToString() + "] " + Type.ToString() + ": P" + Player.ToString() + ", C" + Column.ToString());
        }

        public Fraction LinearOffset
        {
            get
            {
                return linearOffset;
            }
            set
            {
                linearOffset = value;
                LinearOffsetInitialized = true;
                if (linearOffset.Denominator == 0)
                    LinearOffsetInitialized = false;
            }
        }

        public Fraction MetricOffset
        {
            get
            {
                return metricOffset;
            }
            set
            {
                metricOffset = value;
                MetricOffsetInitialized = true;
                if (metricOffset.Denominator == 0)
                    MetricOffsetInitialized = false;
            }
        }

        // some functions don't really care what kind of offset there is as long as there's some way
        // to sort them by time- this property should remain read-only for this reason
        public Fraction Offset
        {
            get
            {
                if (linearOffset.Denominator != 0)
                {
                    return linearOffset;
                }
                else if (metricOffset.Denominator != 0)
                {
                    return metricOffset;
                }
                else
                {
                    return new Fraction(0, 1);
                }
            }
        }

        public Fraction Value
        {
            get
            {
                return this.value;
            }
            set
            {
                this.value = value;
                ValueInitialized = true;
                if (this.value.Denominator == 0)
                    ValueInitialized = false;
            }
        }
    }

    public enum EntryType
    {
        Invalid,
        Marker,
        Sample,
        Tempo,
        Measure,
        Mine,
        Event,
        Judgement,
        BGA,
        EndOfSong
    }
}
