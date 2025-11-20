(module
  (import "env" "print_i32" (func $print_i32 (param i32)))
  (import "env" "print_f64" (func $print_f64 (param f64)))
  
  (func $setfirst (param $a i32)
    (i32.const 111)
  )
  
  (func $main (export "main")
    (local $x i32)
    (i32.const 10)
    (i32.const 20)
    (i32.const 30)
    (local.get $x)
    (call $setfirst)
    (call $print_i32)
  )
  
)
