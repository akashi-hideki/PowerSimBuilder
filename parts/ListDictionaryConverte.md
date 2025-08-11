# ListDictionaryConverter 仕様書 (Markdown形式)


- **目的**: 文字列リストからオブジェクトを再構築する際のチャンクサイズを決定
- **デフォルト動作**: 1要素=1オブジェクト
- **カスタマイズ例**:
  ```csharp
  public class CustomConverter : ListDictionaryConverter<Person, Employee>
  {
      protected override int GetChunkSize(List<string> list, int startIndex)
      {
          // 最初の要素にチャンクサイズが格納されている形式
          return int.Parse(list[startIndex]);
      }
  }
  ```

## エラーハンドリング

- 必要な変換プロパティが設定されていない場合、`InvalidOperationException`がスローされます
- エラーメッセージ例:
  `"ConvertT1ToT2 コンバーターが初期化されていません。使用前にプロパティを設定してください。"`

## 使用例

### 基本設定

```csharp
var converter = new ListDictionaryConverter<Person, Employee>();

// T1⇄T2変換設定
converter.ConvertT1ToT2 = p => new Employee { 
    Id = p.Id, 
    FullName = $"{p.LastName} {p.FirstName}" 
};
converter.ConvertT2ToT1 = e => new Person {
    Id = e.Id,
    LastName = e.FullName.Split(' ')[0],
    FirstName = e.FullName.Split(' ')[1]
};

// 変換実行
var personDict = ...;
var employeeDict = converter.ConvertT1ToT2Dictionary(personDict);
```

### カスタムシリアライゼーション

```csharp
// T1⇄文字列リスト変換設定
converter.ConvertT1ToStringList = p => 
    new List<string> { p.Id.ToString(), p.LastName, p.FirstName };
  
converter.ConvertStringListToT1 = list => 
    new Person {
        Id = int.Parse(list[0]),
        LastName = list[1],
        FirstName = list[2]
    };

// カスタムチャンクサイズ
public class PersonConverter : ListDictionaryConverter<Person, Employee>
{
    protected override int GetChunkSize(List<string> list, int startIndex)
    {
        return 3; // ID, LastName, FirstNameの3要素
    }
}

// シリアライゼーション実行
var serializedDict = converter.ConvertT1ToStringDictionary(personDict);
```

## 注意事項

1. **プロパティの事前設定**: 各変換メソッドを呼び出す前に、対応する変換プロパティを設定する必要があります
2. **チャンク分割の重要性**:
   - 文字列リスト→オブジェクト変換時はチャンク分割ロジックが重要
   - 複数要素で1オブジェクトを構成する場合は`GetChunkSize`のオーバーライド必須
3. **パフォーマンス考慮**:
   - 大規模データではLINQの遅延評価を活用
   - チャンク処理はストリーミング処理(yield return)で実装

```

この仕様書とコメント付きコードにより、クラスの使用方法やカスタマイズ方法を明確に理解できます。特に重要なのは：
1. プロパティで変換ロジックを注入する設計
2. チャンク分割が必要な場合のカスタマイズ方法
3. 各メソッドの事前条件と例外処理

必要に応じて`GetChunkSize`メソッドをオーバーライドすることで、CSVや固定長バイナリなど様々なデータ形式に対応可能です。
```
