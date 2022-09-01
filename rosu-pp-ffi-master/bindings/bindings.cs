// Automatically generated by Interoptopus.

#pragma warning disable 0105
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RosuPP;
#pragma warning restore 0105

namespace RosuPP
{
    public static partial class Rosu
    {
        public const string NativeLib = "rosu_pp_ffi";

        static Rosu()
        {
        }


        /// Destroys the given instance.
        ///
        /// # Safety
        ///
        /// The passed parameter MUST have been created with the corresponding init function;
        /// passing any other value results in undefined behavior.
        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "calculator_destroy")]
        public static extern FFIError calculator_destroy(ref IntPtr context);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "calculator_new")]
        public static extern FFIError calculator_new(ref IntPtr context, string beatmap_path);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "calculator_calculate")]
        public static extern CalculateResult calculator_calculate(IntPtr context, IntPtr score_params);

        /// Destroys the given instance.
        ///
        /// # Safety
        ///
        /// The passed parameter MUST have been created with the corresponding init function;
        /// passing any other value results in undefined behavior.
        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_destroy")]
        public static extern FFIError score_params_destroy(ref IntPtr context);

        /// 构造一个params
        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_new")]
        public static extern FFIError score_params_new(ref IntPtr context);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_mode")]
        public static extern void score_params_mode(IntPtr context, Mode mode);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_mods")]
        public static extern void score_params_mods(IntPtr context, uint mods);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_acc")]
        public static extern void score_params_acc(IntPtr context, double acc);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_n300")]
        public static extern void score_params_n300(IntPtr context, uint n300);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_n100")]
        public static extern void score_params_n100(IntPtr context, uint n100);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_n50")]
        public static extern void score_params_n50(IntPtr context, uint n50);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_combo")]
        public static extern void score_params_combo(IntPtr context, uint combo);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_score")]
        public static extern void score_params_score(IntPtr context, uint score);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_n_misses")]
        public static extern void score_params_n_misses(IntPtr context, uint n_misses);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_n_katu")]
        public static extern void score_params_n_katu(IntPtr context, uint n_katu);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_passed_objects")]
        public static extern void score_params_passed_objects(IntPtr context, uint passed_objects);

        [DllImport(NativeLib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "score_params_clock_rate")]
        public static extern void score_params_clock_rate(IntPtr context, double clock_rate);

    }

    public enum Mode
    {
        /// osu!standard
        Osu = 0,
        /// osu!taiko
        Taiko = 1,
        /// osu!catch
        Catch = 2,
        /// osu!mania
        Mania = 3,
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct CalculateResult
    {
        public byte mode;
        public double stars;
        public double pp;
        public Optionf64 ppAcc;
        public Optionf64 ppAim;
        public Optionf64 ppFlashlight;
        public Optionf64 ppSpeed;
        public Optionf64 ppStrain;
        public Optionu32 nFruits;
        public Optionu32 nDroplets;
        public Optionu32 nTinyDroplets;
        public Optionf64 aimStrain;
        public Optionf64 speedStrain;
        public Optionf64 flashlightRating;
        public Optionf64 sliderFactor;
        public double ar;
        public double cs;
        public double hp;
        public double od;
        public double bpm;
        public double clockRate;
        public Optionf64 timePreempt;
        public Optionf64 greatHitWindow;
        public Optionu32 nCircles;
        public Optionu32 nSliders;
        public Optionu32 nSpinners;
        public Optionu32 maxCombo;
    }

    public enum FFIError
    {
        Ok = 0,
        Null = 100,
        Panic = 200,
        Fail = 300,
    }

    ///Option type containing boolean flag and maybe valid data.
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Optionf64
    {
        ///Element that is maybe valid.
        double t;
        ///Byte where `1` means element `t` is valid.
        byte is_some;
    }

    public partial struct Optionf64
    {
        public static Optionf64 FromNullable(double? nullable)
        {
            var result = new Optionf64();
            if (nullable.HasValue)
            {
                result.is_some = 1;
                result.t = nullable.Value;
            }

            return result;
        }

        public double? ToNullable()
        {
            return this.is_some == 1 ? this.t : (double?)null;
        }
    }


    ///Option type containing boolean flag and maybe valid data.
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Optionu32
    {
        ///Element that is maybe valid.
        uint t;
        ///Byte where `1` means element `t` is valid.
        byte is_some;
    }

    public partial struct Optionu32
    {
        public static Optionu32 FromNullable(uint? nullable)
        {
            var result = new Optionu32();
            if (nullable.HasValue)
            {
                result.is_some = 1;
                result.t = nullable.Value;
            }

            return result;
        }

        public uint? ToNullable()
        {
            return this.is_some == 1 ? this.t : (uint?)null;
        }
    }



    public partial class Calculator : IDisposable
    {
        private IntPtr _context;

        private Calculator() {}

        public static Calculator New(string beatmap_path)
        {
            var self = new Calculator();
            var rval = Rosu.calculator_new(ref self._context, beatmap_path);
            if (rval != FFIError.Ok)
            {
                throw new InteropException<FFIError>(rval);
            }
            return self;
        }

        public void Dispose()
        {
            var rval = Rosu.calculator_destroy(ref _context);
            if (rval != FFIError.Ok)
            {
                throw new InteropException<FFIError>(rval);
            }
        }

        public CalculateResult Calculate(IntPtr score_params)
        {
            return Rosu.calculator_calculate(_context, score_params);
        }

        public IntPtr Context => _context;
    }


    public partial class ScoreParams : IDisposable
    {
        private IntPtr _context;

        private ScoreParams() {}

        /// 构造一个params
        public static ScoreParams New()
        {
            var self = new ScoreParams();
            var rval = Rosu.score_params_new(ref self._context);
            if (rval != FFIError.Ok)
            {
                throw new InteropException<FFIError>(rval);
            }
            return self;
        }

        public void Dispose()
        {
            var rval = Rosu.score_params_destroy(ref _context);
            if (rval != FFIError.Ok)
            {
                throw new InteropException<FFIError>(rval);
            }
        }

        public void Mode(Mode mode)
        {
            Rosu.score_params_mode(_context, mode);
        }

        public void Mods(uint mods)
        {
            Rosu.score_params_mods(_context, mods);
        }

        public void Acc(double acc)
        {
            Rosu.score_params_acc(_context, acc);
        }

        public void N300(uint n300)
        {
            Rosu.score_params_n300(_context, n300);
        }

        public void N100(uint n100)
        {
            Rosu.score_params_n100(_context, n100);
        }

        public void N50(uint n50)
        {
            Rosu.score_params_n50(_context, n50);
        }

        public void Combo(uint combo)
        {
            Rosu.score_params_combo(_context, combo);
        }

        public void Score(uint score)
        {
            Rosu.score_params_score(_context, score);
        }

        public void NMisses(uint n_misses)
        {
            Rosu.score_params_n_misses(_context, n_misses);
        }

        public void NKatu(uint n_katu)
        {
            Rosu.score_params_n_katu(_context, n_katu);
        }

        public void PassedObjects(uint passed_objects)
        {
            Rosu.score_params_passed_objects(_context, passed_objects);
        }

        public void ClockRate(double clock_rate)
        {
            Rosu.score_params_clock_rate(_context, clock_rate);
        }

        public IntPtr Context => _context;
    }



    public class InteropException<T> : Exception
    {
        public T Error { get; private set; }

        public InteropException(T error): base($"Something went wrong: {error}")
        {
            Error = error;
        }
    }

}