# Pythonシミュレーションプログラミングテキスト（C/C#経験者向け）

## はじめに

このテキストは、C言語やC#の知識がある方を対象に、Pythonでのシミュレーション開発に焦点を当てています。グラフ表示よりも計算・演算を中心に解説します。

## 1. Pythonの基本文法とC/C#との違い

### 1.1 変数宣言と型

```python
# C#: int x = 10;
# Pythonでは型宣言が不要
x = 10
y = 3.14
name = "Python"

# 型ヒント（Python 3.5+）
x: int = 10
y: float = 3.14
```

### 1.2 基本的な制御構造

```python
# 条件分岐
if x > 5:
    print("xは5より大きい")
elif x == 5:
    print("xは5です")
else:
    print("xは5より小さい")

# ループ
for i in range(5):  # 0から4まで
    print(i)

# whileループ
while x > 0:
    print(x)
    x -= 1
```

## 2. Pythonのクラス

### 2.1 基本的なクラス定義

```python
class Particle:
    # コンストラクタ (C#のコンストラクタに相当)
    def __init__(self, mass, position, velocity):
        self.mass = mass          # 質量
        self.position = position  # 位置
        self.velocity = velocity  # 速度
      
    # メソッド定義
    def kinetic_energy(self):
        return 0.5 * self.mass * (self.velocity ** 2)
  
    # 文字列表現 (C#のToString()に相当)
    def __str__(self):
        return f"Particle(mass={self.mass}, pos={self.position}, vel={self.velocity})"

# 使用例
p = Particle(1.0, 0.0, 10.0)
print(p)
print("運動エネルギー:", p.kinetic_energy())
```

### 2.2 継承

```python
class ChargedParticle(Particle):  # Particleクラスを継承
    def __init__(self, mass, position, velocity, charge):
        super().__init__(mass, position, velocity)  # 親クラスのコンストラクタ呼び出し
        self.charge = charge  # 電荷
      
    def __str__(self):
        return f"ChargedParticle(mass={self.mass}, pos={self.position}, vel={self.velocity}, charge={self.charge})"

# 使用例
cp = ChargedParticle(1.0, 0.0, 10.0, 1.6e-19)
print(cp)
```

## 3. シミュレーションの実装例

### 3.1 物理エンジンの基本クラス

```python
import math

class PhysicsEngine:
    def __init__(self, gravity=9.81):
        self.gravity = gravity
        self.particles = []
      
    def add_particle(self, particle):
        self.particles.append(particle)
      
    def update(self, dt):
        """dt秒後の状態を更新"""
        for particle in self.particles:
            # 簡単な重力シミュレーション
            particle.velocity += self.gravity * dt
            particle.position += particle.velocity * dt
          
    def simulate(self, duration, dt):
        """duration秒間のシミュレーションをdt秒刻みで実行"""
        steps = int(duration / dt)
        for step in range(steps):
            self.update(dt)
            print(f"Time: {step*dt:.2f}s")
            for i, p in enumerate(self.particles):
                print(f"  Particle {i}: pos={p.position:.2f}, vel={p.velocity:.2f}")

# シミュレーションの実行
engine = PhysicsEngine()
engine.add_particle(Particle(1.0, 0.0, 0.0))
engine.add_particle(Particle(2.0, 10.0, -5.0))

engine.simulate(duration=5.0, dt=0.1)
```

**数学的概念：自由落下運動**

自由落下運動は重力加速度によって記述されます。位置と速度の関係は以下のように表せます：

```
v(t) = v₀ + g*t
y(t) = y₀ + v₀*t + (1/2)*g*t²
```

ここで：
- v₀：初速度
- y₀：初期位置
- g：重力加速度（通常 -9.81 m/s²）
- t：時間

![自由落下運動の図解](https://via.placeholder.com/400x200?text=Free+Fall+Motion+Diagram)

### 3.2 より複雑なシミュレーション例（衝突検出）

```python
class CollisionPhysicsEngine(PhysicsEngine):
    def __init__(self, gravity=9.81, floor_height=0.0):
        super().__init__(gravity)
        self.floor_height = floor_height
      
    def update(self, dt):
        super().update(dt)
      
        # 床との衝突検出と処理
        for particle in self.particles:
            if particle.position <= self.floor_height:
                particle.position = self.floor_height
                particle.velocity = -particle.velocity * 0.8  # 反発係数
              
    def check_collisions(self):
        """粒子間の衝突検出（簡易版）"""
        for i in range(len(self.particles)):
            for j in range(i+1, len(self.particles)):
                p1 = self.particles[i]
                p2 = self.particles[j]
              
                # ここでは簡単のため距離ベースの衝突検出
                distance = abs(p1.position - p2.position)
                if distance < 0.5:  # 衝突閾値
                    self.resolve_collision(p1, p2)
                  
    def resolve_collision(self, p1, p2):
        """衝突解決（運動量保存則）"""
        # 簡単のため一次元衝突
        m1, m2 = p1.mass, p2.mass
        v1, v2 = p1.velocity, p2.velocity
      
        # 速度の交換（完全弾性衝突）
        new_v1 = ((m1 - m2) * v1 + 2 * m2 * v2) / (m1 + m2)
        new_v2 = ((m2 - m1) * v2 + 2 * m1 * v1) / (m1 + m2)
      
        p1.velocity = new_v1
        p2.velocity = new_v2
```

**数学的概念：弾性衝突**

完全弾性衝突では運動量と運動エネルギーが保存されます。一次元衝突の場合、衝突後の速度は以下の式で計算できます：

```
v₁' = ((m₁ - m₂) * v₁ + 2 * m₂ * v₂) / (m₁ + m₂)
v₂' = ((m₂ - m₁) * v₂ + 2 * m₁ * v₁) / (m₁ + m₂)
```

![弾性衝突の図解](https://via.placeholder.com/400x200?text=Elastic+Collision+Diagram)

## 4. パフォーマンス最適化

### 4.1 リスト内包表記

```python
# 従来の方法
squares = []
for i in range(10):
    squares.append(i * i)

# リスト内包表記（より高速）
squares = [i * i for i in range(10)]
```

### 4.2 NumPyを使用した数値計算の高速化

```python
import numpy as np

# Pythonリスト（遅い）
positions = [p.position for p in particles]
velocities = [p.velocity for p in particles]

# NumPy配列（高速）
positions_np = np.array(positions)
velocities_np = np.array(velocities)

# ベクトル演算（全粒子に対して一括処理）
new_positions = positions_np + velocities_np * dt
```

**数学的概念：ベクトル演算**

NumPyはベクトルと行列演算を効率化します。ベクトル化された演算は、ループを使用する代わりに、配列全体に対して一度に操作を適用します：

```
位置の更新: p⃗(t+Δt) = p⃗(t) + v⃗(t)*Δt
速度の更新: v⃗(t+Δt) = v⃗(t) + a⃗(t)*Δt
```

![ベクトル演算の図解](https://via.placeholder.com/400x200?text=Vector+Operations+Diagram)

## 5. 高度なクラス機能

### 5.1 プロパティ（getter/setter）

```python
class SmartParticle:
    def __init__(self, mass):
        self._mass = mass  # プライベート変数（慣例）
      
    @property
    def mass(self):
        """getter"""
        return self._mass
      
    @mass.setter
    def mass(self, value):
        """setter"""
        if value <= 0:
            raise ValueError("質量は正の値である必要があります")
        self._mass = value

# 使用例
p = SmartParticle(10.0)
print(p.mass)  # getter呼び出し
p.mass = 20.0  # setter呼び出し
```

### 5.2 静的メソッドとクラスメソッド

```python
class Vector:
    def __init__(self, x, y):
        self.x = x
        self.y = y
      
    @staticmethod
    def zero():
        """静的メソッド：インスタンスに依存しない"""
        return Vector(0, 0)
      
    @classmethod
    def from_points(cls, p1, p2):
        """クラスメソッド：代替コンストラクタ"""
        return cls(p2.x - p1.x, p2.y - p1.y)
```

## 6. シミュレーションの応用

### 6.1 モンテカルロシミュレーション

```python
import random

class MonteCarloSimulator:
    def __init__(self):
        self.results = []
      
    def simulate_pi(self, num_samples):
        """円周率のモンテカルロ推定"""
        inside_circle = 0
      
        for _ in range(num_samples):
            x = random.uniform(-1, 1)
            y = random.uniform(-1, 1)
          
            # 単位円内にあるかチェック
            if x**2 + y**2 <= 1:
                inside_circle += 1
              
        pi_estimate = 4 * inside_circle / num_samples
        return pi_estimate

# 使用例
simulator = MonteCarloSimulator()
pi_estimate = simulator.simulate_pi(1000000)
print(f"円周率の推定値: {pi_estimate}")
```

**数学的概念：モンテカルロ法**

モンテカルロ法は乱数を使用して数学的問題を解く統計的手法です。円周率の推定では、単位正方形内にランダムな点を生成し、単位円内にある点の割合からπを推定します：

```
π ≈ 4 * (円内の点数) / (総点数)
```

![モンテカルロ法の図解](https://via.placeholder.com/400x400?text=Monte+Carlo+Pi+Estimation)

単位円の面積はπ、それを囲む正方形の面積は4なので、円内の点の割合に4を乗じるとπの推定値が得られます。

# Pythonシミュレーションプログラミング：クラスとNumPyの詳細解説

## クラスの詳細な動作とNumPy活用

### クラスの基本概念と動作

PythonのクラスはC++やC#のクラスと概念的には似ていますが、いくつかの重要な違いがあります。

```python
class SimulationEntity:
    # クラス変数（すべてのインスタンスで共享）
    entity_count = 0
  
    def __init__(self, mass, position):
        # インスタンス変数（各インスタンス固有）
        self.mass = mass
        self.position = position
        self.velocity = 0.0
      
        # クラス変数を更新
        SimulationEntity.entity_count += 1
        self.id = SimulationEntity.entity_count
  
    def apply_force(self, force, dt):
        """力を適用して状態を更新（F = ma）"""
        acceleration = force / self.mass
        self.velocity += acceleration * dt
        self.position += self.velocity * dt
  
    def get_kinetic_energy(self):
        """運動エネルギーを計算"""
        return 0.5 * self.mass * self.velocity ** 2
  
    @classmethod
    def get_entity_count(cls):
        """クラスメソッド：エンティティの総数を返す"""
        return cls.entity_count
  
    @staticmethod
    def is_valid_mass(mass):
        """静的メソッド：質量が有効かチェック"""
        return mass > 0
  
    def __str__(self):
        """文字列表現を定義"""
        return f"Entity {self.id}: mass={self.mass}, pos={self.position}, vel={self.velocity}"

# クラスの使用例
entity1 = SimulationEntity(1.0, 0.0)
entity2 = SimulationEntity(2.0, 10.0)

print(entity1)
print(entity2)

# 力を適用
entity1.apply_force(10.0, 0.1)
print("運動エネルギー:", entity1.get_kinetic_energy())

# クラスメソッドと静的メソッドの使用
print("総エンティティ数:", SimulationEntity.get_entity_count())
print("質量チェック:", SimulationEntity.is_valid_mass(-1.0))
```

**数学的概念：ニュートンの運動法則**

ニュートンの第二法則は、力と加速度の関係を定義します：

```
F = m * a
```

ここで：
- F：力 (N)
- m：質量 (kg)
- a：加速度 (m/s²)

![ニュートンの運動法則の図解](https://via.placeholder.com/400x200?text=Newton's+Laws+of+Motion)

### NumPyの詳細な使用方法

NumPyは科学技術計算のための強力なライブラリで、多次元配列と高速な数学関数を提供します。

```python
import numpy as np

# 1. 基本的な配列操作
# 配列の作成
arr1 = np.array([1, 2, 3, 4, 5])  # 1次元配列
arr2 = np.array([[1, 2, 3], [4, 5, 6]])  # 2次元配列

print("arr1の形状:", arr1.shape)
print("arr2の形状:", arr2.shape)
print("arr2の次元数:", arr2.ndim)

# 2. 特殊な配列の作成
zeros = np.zeros((3, 3))  # 3x3のゼロ配列
ones = np.ones((2, 4))    # 2x4の1配列
eye = np.eye(3)           # 3x3の単位行列
range_arr = np.arange(0, 10, 2)  # 0から10まで2刻み
linspace_arr = np.linspace(0, 1, 5)  # 0から1まで5等分

print("zeros:\n", zeros)
print("linspace_arr:", linspace_arr)

# 3. 配列の演算
a = np.array([1, 2, 3])
b = np.array([4, 5, 6])

print("a + b =", a + b)  # 要素ごとの加算
print("a * b =", a * b)  # 要素ごとの乗算
print("a.dot(b) =", a.dot(b))  # 内積
print("np.sum(a) =", np.sum(a))  # 合計

# 4. ブロードキャスト（形状の異なる配列間の演算）
matrix = np.array([[1, 2, 3], [4, 5, 6]])
vector = np.array([10, 20, 30])

result = matrix + vector  # 各行にvectorが加算される
print("matrix + vector:\n", result)

# 5. ユニバーサル関数（ufunc）
arr = np.array([0, np.pi/2, np.pi])
print("sin(arr):", np.sin(arr))
print("exp(arr):", np.exp(arr))

# 6. 配列のインデックス操作とスライシング
data = np.array([[1, 2, 3, 4], 
                 [5, 6, 7, 8], 
                 [9, 10, 11, 12]])

print("data[1, 2]:", data[1, 2])  # 7
print("data[0:2, 1:3]:\n", data[0:2, 1:3])  # 部分配列

# 7. 条件に基づく選択
condition = data > 5
print("data > 5:\n", condition)
print("data[data > 5]:", data[data > 5])

# 8. 形状の変更
reshaped = data.reshape(2, 6)
print("reshape(2, 6):\n", reshaped)

# 9. 統計計算
print("平均:", np.mean(data))
print("標準偏差:", np.std(data))
print("最大値:", np.max(data))
print("最小値:", np.min(data))
```

**数学的概念：線形代数と統計**

NumPyは線形代数演算と統計計算をサポートしています：

- 内積: `a・b = Σ(a_i * b_i)`
- 平均: `μ = Σx_i / n`
- 標準偏差: `σ = √(Σ(x_i - μ)² / n)`

![線形代数の図解](https://via.placeholder.com/400x200?text=Linear+Algebra+Operations)

### クラスとNumPyを組み合わせたシミュレーション例

```python
import numpy as np

class ParticleSystem:
    def __init__(self, num_particles):
        self.num_particles = num_particles
      
        # NumPy配列で粒子の状態を一括管理
        self.masses = np.ones(num_particles)
        self.positions = np.zeros(num_particles)
        self.velocities = np.zeros(num_particles)
      
    def initialize_random(self):
        """ランダムな初期条件を設定"""
        self.masses = np.random.uniform(0.5, 2.0, self.num_particles)
        self.positions = np.random.uniform(-10, 10, self.num_particles)
        self.velocities = np.random.uniform(-5, 5, self.num_particles)
      
    def apply_gravity(self, gravity, dt):
        """重力を適用"""
        self.velocities += gravity * dt
      
    def update_positions(self, dt):
        """位置を更新"""
        self.positions += self.velocities * dt
      
    def handle_collisions(self, floor_height=0.0, restitution=0.8):
        """床との衝突処理"""
        # 床より下にある粒子を検出
        below_floor = self.positions < floor_height
      
        # 位置と速度を修正
        self.positions[below_floor] = floor_height
        self.velocities[below_floor] = -self.velocities[below_floor] * restitution
      
    def calculate_energy(self):
        """システムの全エネルギーを計算"""
        kinetic_energy = 0.5 * self.masses * np.square(self.velocities)
        potential_energy = self.masses * 9.81 * self.positions
        return np.sum(kinetic_energy), np.sum(potential_energy)
  
    def simulate(self, duration, dt, gravity=-9.81):
        """シミュレーションを実行"""
        steps = int(duration / dt)
        energy_history = []
      
        for step in range(steps):
            self.apply_gravity(gravity, dt)
            self.update_positions(dt)
            self.handle_collisions()
          
            # エネルギーを記録
            ke, pe = self.calculate_energy()
            energy_history.append((ke, pe, ke + pe))
          
            if step % 100 == 0:
                print(f"Step {step}: KE={ke:.2f}, PE={pe:.2f}, Total={ke+pe:.2f}")
              
        return energy_history

# シミュレーションの実行
system = ParticleSystem(1000)
system.initialize_random()
energy_history = system.simulate(duration=5.0, dt=0.01)

# エネルギー保存則の確認（少し減るのは衝突によるエネルギー散逸のため）
initial_energy = energy_history[0][2]
final_energy = energy_history[-1][2]
print(f"初期エネルギー: {initial_energy:.2f}")
print(f"最終エネルギー: {final_energy:.2f}")
print(f"エネルギー減少率: {(initial_energy - final_energy) / initial_energy * 100:.2f}%")
```

**数学的概念：エネルギー保存則**

物理システムでは、運動エネルギーと位置エネルギーの和が保存されます（非保存力が働かない場合）：

```
E_total = E_kinetic + E_potential = 定数
```

運動エネルギー: `E_k = 1/2 * m * v²`
位置エネルギー: `E_p = m * g * h`

![エネルギー保存則の図解](https://via.placeholder.com/400x200?text=Energy+Conservation+Principle)

### 高度なクラス機能：プロパティと特殊メソッド

```python
class Vector:
    def __init__(self, x, y, z):
        self._data = np.array([x, y, z], dtype=float)
  
    @property
    def x(self):
        return self._data[0]
  
    @x.setter
    def x(self, value):
        self._data[0] = value
  
    @property
    def y(self):
        return self._data[1]
  
    @y.setter
    def y(self, value):
        self._data[1] = value
  
    @property
    def z(self):
        return self._data[2]
  
    @z.setter
    def z(self, value):
        self._data[2] = value
  
    @property
    def magnitude(self):
        return np.linalg.norm(self._data)
  
    def normalize(self):
        norm = self.magnitude
        if norm > 0:
            self._data /= norm
  
    # 算術演算のオーバーロード
    def __add__(self, other):
        return Vector(*(self._data + other._data))
  
    def __sub__(self, other):
        return Vector(*(self._data - other._data))
  
    def __mul__(self, scalar):
        return Vector(*(self._data * scalar))
  
    def __truediv__(self, scalar):
        return Vector(*(self._data / scalar))
  
    def __str__(self):
        return f"Vector({self.x}, {self.y}, {self.z})"
  
    def __repr__(self):
        return f"Vector({self.x}, {self.y}, {self.z})"
  
    def dot(self, other):
        return np.dot(self._data, other._data)
  
    def cross(self, other):
        return Vector(*np.cross(self._data, other._data))

# ベクトルクラスの使用例
v1 = Vector(1, 2, 3)
v2 = Vector(4, 5, 6)

print("v1 =", v1)
print("v2 =", v2)
print("v1 + v2 =", v1 + v2)
print("v1 * 2 =", v1 * 2)
print("v1の大きさ =", v1.magnitude)
print("内積 =", v1.dot(v2))
print("外積 =", v1.cross(v2))

# 正規化
v1.normalize()
print("正規化後:", v1)
print("正規化後の大きさ:", v1.magnitude)
```

**数学的概念：ベクトル演算**

3次元ベクトルには重要な演算がいくつかあります：

- 大きさ: `|v| = √(x² + y² + z²)`
- 内積: `v・w = x₁x₂ + y₁y₂ + z₁z₂`
- 外積: `v×w = (y₁z₂ - z₁y₂, z₁x₂ - x₁z₂, x₁y₂ - y₁x₂)`

![ベクトル演算の図解](https://via.placeholder.com/400x300?text=Vector+Operations+in+3D)

### パフォーマンス最適化：NumPyのベクトル化演算

```python
import time

# パフォーマンス比較: ループ vs ベクトル化
def performance_comparison():
    size = 1000000
    data = np.random.rand(size)
  
    # ループを使用した計算
    start_time = time.time()
    result_loop = np.zeros(size)
    for i in range(size):
        result_loop[i] = data[i] ** 2 + np.sin(data[i])
    loop_time = time.time() - start_time
  
    # ベクトル化された計算
    start_time = time.time()
    result_vectorized = data ** 2 + np.sin(data)
    vectorized_time = time.time() - start_time
  
    # 結果の検証
    error = np.max(np.abs(result_loop - result_vectorized))
  
    print(f"サイズ: {size}")
    print(f"ループ時間: {loop_time:.4f}秒")
    print(f"ベクトル化時間: {vectorized_time:.4f}秒")
    print(f"速度向上: {loop_time/vectorized_time:.2f}倍")
    print(f"最大誤差: {error:.2e}")

performance_comparison()
```

**数学的概念：ベクトル化の利点**

ベクトル化は、要素ごとの演算をループではなく配列全体に対する操作として表現することで、大幅な性能向上をもたらします。NumPyは内部で高度に最適化されたCコードを使用するため、Pythonのループよりもはるかに高速です。

![ベクトル化のパフォーマンス比較図](https://via.placeholder.com/400x300?text=Vectorization+Performance+Comparison)

## まとめ

このテキストでは、C/C#経験者を対象に、Pythonでのシミュレーションプログラミングについて解説しました。基本的な文法の違いから、物理シミュレーションの実装、パフォーマンス最適化までを網羅しました。NumPyを活用したベクトル化演算や、物理学の基本概念を理解することで、効率的で正確なシミュレーションを開発できるようになります。

数学的概念を視覚的に理解するために、図解を交えながら解説しました。これらの概念を実際のコーディングに活かし、より高度なシミュレーション開発に挑戦してみてください。