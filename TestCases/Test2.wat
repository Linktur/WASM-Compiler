(module
  (import "env" "print_i32" (func $print_i32 (param i32)))
  (import "env" "print_f64" (func $print_f64 (param f64)))
  
  (func $main (export "main")
    (local $r f64)
    (i32.const 5)
    (local.set $r)
    (local.get $r)
    (call $print_i32)
  )
  
)
