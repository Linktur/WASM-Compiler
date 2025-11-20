(module
  (import "env" "print_i32" (func $print_i32 (param i32)))
  (import "env" "print_f64" (func $print_f64 (param f64)))
  
  (func $main (export "main")
    (local $i i32)
    (i32.const 1)
    (local.set $i)
    (block $break
      (loop $continue
        (local.get $i)
        (i32.const 3)
        (i32.gt_s)
        (br_if $break)
        (local.get $i)
        (call $print_i32)
        (local.get $i)
        (i32.const 1)
        (i32.add)
        (local.set $i)
        (br $continue)
      )
    )
  )
  
)
