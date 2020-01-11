namespace Utf8JsonStreamReader.Test

open FsUnit
open System.Diagnostics
open Xunit

// although FsUnitTyped is available with NUnit, it isn't with Xunit.
// without it you are provided with a lovely foot-gun that lets you compare
// objects of different types. With it the compiler can do its job and keep your
// feet intact.
module FsUnitTyped =

    [<DebuggerStepThrough>]
    let shouldEqual (expected : 'a) (actual : 'a) = actual |> should equal expected

    [<DebuggerStepThrough>]
    let shouldEqualTol  (tol : double) (expected : double) (actual : double) = actual |> should (equalWithin tol) expected

    [<DebuggerStepThrough>]
    let shouldNotEqual (expected : 'a) (actual : 'a) = actual |> should not' (equal expected)

    [<DebuggerStepThrough>]
    let shouldContain (x : 'a) (y : 'a seq) =
        Assert.Contains(x, y)

    [<DebuggerStepThrough>]
    let shouldBeEmpty (xs : 'a seq) = Assert.Empty xs

    [<DebuggerStepThrough>]
    let shouldNotContain (x : 'a) (y : 'a seq) = Assert.DoesNotContain(x, y)

    [<DebuggerStepThrough>]
    let shouldBeSmallerThan (expected : 'a) (actual : 'a) = actual |> should be (lessThan expected)

    [<DebuggerStepThrough>]
    let shouldBeGreaterThan (expected : 'a) (actual : 'a) = actual |> should be (greaterThan expected)

    [<DebuggerStepThrough>]
    let shouldFail<'exn when 'exn :> exn>(f : unit -> unit) : 'exn =
        Assert.Throws<'exn> f

    [<DebuggerStepThrough>]
    let shouldContainText (expectedSubstring : string) (actualString : string) =
        Assert.Contains(expectedSubstring, actualString)

    [<DebuggerStepThrough>]
    let shouldNotContainText (expectedSubstring : string) (actualString : string) =
        Assert.DoesNotContain(expectedSubstring, actualString)

    [<DebuggerStepThrough>]
    let shouldHaveLength (expected : int) (items : 'a seq) =
        items |> Seq.length |> should equal expected

    [<DebuggerStepThrough>]
    let shouldEqualReferenceOf (expected : 'a) (actual : 'a) = Assert.True(obj.ReferenceEquals(expected, actual))

    [<DebuggerStepThrough>]
    let shouldNotEqualReferenceOf (expected : 'a) (actual : 'a) = Assert.False(obj.ReferenceEquals(expected, actual))
