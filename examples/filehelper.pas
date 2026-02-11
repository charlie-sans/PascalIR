program ReadEmbedded;
uses SysUtils;
var
  data_start, data_end: Pointer; external name '_binary_data_txt_start';
  data_end2: Pointer; external name '_binary_data_txt_end';
  len: NativeUInt;
  s: AnsiString;
begin
  len := NativeUInt(data_end2) - NativeUInt(data_start);
  SetString(s, PAnsiChar(data_start), len);
  Writeln(s);
end.