use crate::*;
use interoptopus::{
    ffi_service, ffi_service_ctor, ffi_service_method, ffi_type, patterns::slice::FFISlice
};
use rosu_pp::{AnyPP, Beatmap};

#[ffi_type(opaque)]
#[derive(Default)]
pub struct Calculator {
    pub inner: Beatmap,
}

// Regular implementation of methods.
#[ffi_service(error = "FFIError", prefix = "calculator_")]
impl Calculator {
    #[ffi_service_ctor]
    pub fn new(beatmap_data: FFISlice<u8>) -> Result<Self, Error> {
        Ok(Self {
            inner: Beatmap::from_bytes(beatmap_data.as_slice())?,
        })
    }

    #[ffi_service_method(on_panic = "undefined_behavior")]
    pub fn calculate(&mut self, score_params: ScoreParams) -> CalculateResult {
        let mods = score_params.mods;
        let clock_rate = score_params.clockRate;
        let calculator = score_params.apply(AnyPP::new(&self.inner));
        CalculateResult::new(
            calculator.calculate(),
            &self.inner,
            mods,
            clock_rate.into_option(),
        )
    }
}