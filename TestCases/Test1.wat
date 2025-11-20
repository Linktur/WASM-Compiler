(module
  (import "env" "print_i32" (func $print_i32 (param i32)))
  (import "env" "print_f64" (func $print_f64 (param f64)))
  
  (func $main (export "main")
    (i32.const 42)
    (call $print_i32)
    (f64.const 3.14)
    (call $print_f64)
    (i32.const 1)
    (call $print_i32)
  )
  
)
