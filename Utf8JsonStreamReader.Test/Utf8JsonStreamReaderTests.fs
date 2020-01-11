namespace Utf8JsonStreamReader.Test

open System.IO
open System.Text
open System.Text.Json
open Xunit
open Xunit.Abstractions
open FsUnitTyped
open Utf8JsonStreamReader

// the simplest possible type we can serialise
[<CLIMutable>]
type Data =
    {
        F : string
    }
    
type Utf8JsonStreamReaderTests(output : ITestOutputHelper) =
    let serializerOptions = JsonSerializerOptions(AllowTrailingCommas=false, WriteIndented=false)
    
    let streamOfString (s : string) =
        let stream = new MemoryStream(s.Length)
        do
            use writer = new StreamWriter(stream, UTF8Encoding(false), 0x10000, true)
            writer.Write s
        stream.Position <- 0L
        stream

    let bufferSize = 16
        
    [<Fact>]
    member this.``Ensure can roundtrip data test type without stream helper`` () =
        // make sure we'll have no problems with our trivial type
        let json = JsonSerializer.Serialize<Data>({ F = "a" }, serializerOptions)
        output.WriteLine <| sprintf "json: %s" json
        let v : Data = JsonSerializer.Deserialize(json, serializerOptions)
        v |> shouldEqual { F = "a" }
        
    // note that buffers are typically powers of 2 even if we request < that.
    // so write these tests with that in mind
    // {"F":"abcdefgh"} is 16 chars
    
    [<Fact>]
    member this.``Ensure can deserialise single object json of length = bufferSize-1`` () =
        let json = """{"F":"abcdefg"}"""
        json.Length |> shouldEqual (bufferSize-1)
        
        use stream = streamOfString json

        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            use doc = r.GetJsonDocument()
            doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdefg"
            r.Read() |> shouldEqual false
        finally
            r.Dispose()

    [<Fact>]
    member this.``Ensure can deserialise single object json of length < bufferSize`` () =
        let json = """{"F":"abcde"}"""
        json.Length |> shouldBeSmallerThan bufferSize
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            use doc = r.GetJsonDocument()
            doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcde"
            r.Read() |> shouldEqual false
        finally
            r.Dispose()

    [<Fact>]
    member this.``Ensure can deserialise single object json of length = bufferSize`` () =
        let json = """{"F":"abcdefgh"}"""
        json.Length |> shouldEqual bufferSize
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            use doc = r.GetJsonDocument()
            doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdefgh"
            r.Read() |> shouldEqual false
        finally
            r.Dispose()

    [<Fact>]
    member this.``Ensure can deserialise single object json of length = bufferSize+1`` () =
        let json = """{"F":"abcdefghi"}"""
        json.Length |> shouldEqual (bufferSize+1)
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject

            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdefghi"
            r.TokenType |> shouldEqual JsonTokenType.EndObject
                
            r.Read() |> shouldEqual false

        finally
            r.Dispose()

    [<Fact>]
    member this.``Ensure can deserialise single object json of length = bufferSize+2`` () =
        let json = """{"F":"abcdefghij"}"""
        json.Length |> shouldEqual (bufferSize+2)
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject

            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdefghij"
            r.TokenType |> shouldEqual JsonTokenType.EndObject
                
            r.Read() |> shouldEqual false

        finally
            r.Dispose()

    [<Fact>]
    member this.``Ensure can deserialise single object json of length = bufferSize+3 - string token crossing buffer boundary`` () =
        let json = """{"F":"abcdefghijk"}"""
        json.Length |> shouldEqual (bufferSize+3)
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject

            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdefghijk"
            r.TokenType |> shouldEqual JsonTokenType.EndObject
                
            r.Read() |> shouldEqual false

        finally
            r.Dispose()            

    [<Fact>]
    member this.``Ensure can deserialise single object json of length = bufferSize*1,5`` () =
        let json = """{"F":"abcdefghijklmnop"}"""
        json.Length |> shouldEqual (1.5 * float bufferSize |> int)
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            use doc = r.GetJsonDocument()
            doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdefghijklmnop"
            r.Read() |> shouldEqual false
        finally
            r.Dispose()

    [<Fact>]
    member this.``Ensure can deserialise single object json of length = bufferSize*2`` () =
        // single-char string field needs 9 chars
        let json = """{"F":"abcdefghijklmnopqrstuvwx"}"""
        json.Length |> shouldEqual (bufferSize*2)
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            use doc = r.GetJsonDocument()
            doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdefghijklmnopqrstuvwx"
            r.Read() |> shouldEqual false
        finally
            r.Dispose()

    [<Fact>]
    member this.``Ensure can deserialise single object json of length = bufferSize*2+1`` () =
        // single-char string field needs 9 chars
        let json = """{"F":"abcdefghijklmnopqrstuvwxy"}"""
        json.Length |> shouldEqual (bufferSize*2+1)
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            use doc = r.GetJsonDocument()
            doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdefghijklmnopqrstuvwxy"
            r.Read() |> shouldEqual false
        finally
            r.Dispose()

    [<Fact>]
    member this.``Ensure can deserialise single object json of length = bufferSize*2,5`` () =
        // single-char string field needs 9 chars
        let json = """{"F":"abcdefghijklmnopqrstuvwxyzABCDEF"}"""
        json.Length |> shouldEqual (float bufferSize * 2.5 |> int)
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            use doc = r.GetJsonDocument()
            doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdefghijklmnopqrstuvwxyzABCDEF"
            r.Read() |> shouldEqual false
        finally
            r.Dispose()
            
    [<Fact>]
    member this.``Ensure can deserialise single object json of length = bufferSize*3`` () =
        // single-char string field needs 9 chars
        let json = """{"F":"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMN"}"""
        json.Length |> shouldEqual (float bufferSize * 3.0 |> int)
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            use doc = r.GetJsonDocument()
            doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMN"
            r.Read() |> shouldEqual false
        finally
            r.Dispose()
            
    [<Fact>]
    member this.``Ensure can deserialise single element array json of length < bufferSize`` () =
        let json = """[{"F":"abc"}]"""
        json.Length |> shouldBeSmallerThan bufferSize
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartArray
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            use doc = r.GetJsonDocument()
            doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abc"
            r.TokenType |> shouldEqual JsonTokenType.EndObject
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.EndArray
            r.Read() |> shouldEqual false
        finally
            r.Dispose()
                                   
    [<Fact>]
    member this.``Ensure can deserialise two element array json of length < bufferSize`` () =
        let json = """[{"F":"a"},{"F":"b"}]"""
        let bufferSize = 32
        json.Length |> shouldBeSmallerThan bufferSize
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartArray
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "a"
            r.TokenType |> shouldEqual JsonTokenType.EndObject

            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "b"
            r.TokenType |> shouldEqual JsonTokenType.EndObject
            
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.EndArray
            r.Read() |> shouldEqual false
        finally
            r.Dispose()

    [<Fact>]
    member this.``Ensure can deserialise two element array json of length = bufferSize`` () =
        let json = """[{"F":"aaaaaa"},{"F":"bbbbbbb"}]"""
        let bufferSize = 32
        json.Length |> shouldEqual bufferSize
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartArray
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "aaaaaa"
            r.TokenType |> shouldEqual JsonTokenType.EndObject

            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "bbbbbbb"
            r.TokenType |> shouldEqual JsonTokenType.EndObject
            
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.EndArray
            r.Read() |> shouldEqual false
        finally
            r.Dispose()

    [<Fact>]
    member this.``Ensure can deserialise two element array json of length = bufferSize*2`` () =
        let json = """[{"F":"abcdef"},{"F":"bcdefgh"}]"""
        bufferSize * 2 |> shouldEqual json.Length
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartArray
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdef"
            r.TokenType |> shouldEqual JsonTokenType.EndObject

            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "bcdefgh"
            r.TokenType |> shouldEqual JsonTokenType.EndObject
            
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.EndArray
            r.Read() |> shouldEqual false
        finally
            r.Dispose()

    [<Fact>]
    member this.``Ensure can deserialise when middle element spans a buffer boundary`` () =
        let json = """[{"F":"a"},{"F":"abcdefghijklmnopqrs"},{"F":"z"}]"""
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartArray
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "a"
            r.TokenType |> shouldEqual JsonTokenType.EndObject

            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdefghijklmnopqrs"
            r.TokenType |> shouldEqual JsonTokenType.EndObject
            
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "z"
            r.TokenType |> shouldEqual JsonTokenType.EndObject

            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.EndArray
            r.Read() |> shouldEqual false
        finally
            r.Dispose()
            
    [<Fact>]
    member this.``Ensure can deserialise when middle element spans multiple buffer boundaries`` () =
        let json = """[{"F":"a"},{"F":"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"},{"F":"z"}]"""
        //            0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789ab
        
        use stream = streamOfString json
        let r = new Utf8JsonStreamReader(stream, bufferSize)
        try
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartArray
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "a"
            r.TokenType |> shouldEqual JsonTokenType.EndObject

            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
            r.TokenType |> shouldEqual JsonTokenType.EndObject
            
            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.StartObject
            do
                use doc = r.GetJsonDocument()
                doc.RootElement.GetProperty("F").GetString() |> shouldEqual "z"
            r.TokenType |> shouldEqual JsonTokenType.EndObject

            r.Read() |> shouldEqual true
            r.TokenType |> shouldEqual JsonTokenType.EndArray
            r.Read() |> shouldEqual false
        finally
            r.Dispose()                    