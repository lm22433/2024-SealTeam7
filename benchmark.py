import time
import numpy as np

def sigmoid(x):
    return 1 / (1 + np.exp(-x))

def cubic_bezier(t, p0, p1, p2, p3):
    one_minus_t = 1 - t
    return (
        one_minus_t**3 * p0 +
        3 * t * one_minus_t**2 * p1 +
        3 * t**2 * one_minus_t * p2 +
        t**3 * p3
    )

def cubic_bezier_vectorized(t, p0, p1, p2, p3):
    one_minus_t = 1 - t
    return (
        one_minus_t**3 * p0 +
        3 * t * one_minus_t**2 * p1 +
        3 * t**2 * one_minus_t * p2 +
        t**3 * p3
    )

def smoothstep(x, edge0, edge1):
    x = (x - edge0) / (edge1 - edge0)
    return x * x * (3 - 2 * x)

def smoothstep_vectorized(x, edge0, edge1):
    x = (x - edge0) / (edge1 - edge0)
    return x * x * (3 - 2 * x)

def benchmark_sigmoid(n_points=1000000):
    x = np.linspace(-10, 10, n_points)
    start_time = time.time()
    result = sigmoid(x)
    end_time = time.time()
    return end_time - start_time

def benchmark_bezier(n_points=1000000):
    t = np.linspace(0, 1, n_points)
    start_time = time.time()
    result = [cubic_bezier(t_i, 0, 0.2, 0.8, 1) for t_i in t]
    end_time = time.time()
    return end_time - start_time

def benchmark_bezier_vectorized(n_points=1000000):
    t = np.linspace(0, 1, n_points)
    start_time = time.time()
    result = cubic_bezier_vectorized(t, 0, 0.2, 0.8, 1)
    end_time = time.time()
    return end_time - start_time

def benchmark_smoothstep(n_points=1000000):
    x = np.linspace(-1, 2, n_points)
    start_time = time.time()
    result = [smoothstep(x_i, 0, 1) for x_i in x]
    end_time = time.time()
    return end_time - start_time

def benchmark_smoothstep_vectorized(n_points=1000000):
    x = np.linspace(-1, 2, n_points)
    start_time = time.time()
    result = smoothstep_vectorized(x, 0, 1)
    end_time = time.time()
    return end_time - start_time

if __name__ == "__main__":
    n_points = 1000000
    print(f"Benchmarking with {n_points:,} points:")
    
    # Warm up
    benchmark_sigmoid(1000)
    benchmark_bezier(1000)
    benchmark_bezier_vectorized(1000)
    benchmark_smoothstep(1000)
    benchmark_smoothstep_vectorized(1000)
    
    # Actual benchmark
    sigmoid_time = benchmark_sigmoid(n_points)
    bezier_time = benchmark_bezier(n_points)
    bezier_vectorized_time = benchmark_bezier_vectorized(n_points)
    smoothstep_time = benchmark_smoothstep(n_points)
    smoothstep_vectorized_time = benchmark_smoothstep_vectorized(n_points)
    
    print(f"\nSigmoid time: {sigmoid_time:.4f} seconds")
    print(f"Bezier (loop) time: {bezier_time:.4f} seconds")
    print(f"Bezier (vectorized) time: {bezier_vectorized_time:.4f} seconds")
    print(f"Bezier (loop) is {bezier_time/sigmoid_time:.2f}x slower than sigmoid")
    print(f"Bezier (vectorized) is {bezier_vectorized_time/sigmoid_time:.2f}x slower than sigmoid")
    print(f"Smoothstep time: {smoothstep_time:.4f} seconds")
    print(f"Smoothstep (vectorized) time: {smoothstep_vectorized_time:.4f} seconds")
    print(f"Smoothstep is {smoothstep_time/sigmoid_time:.2f}x slower than sigmoid")
    print(f"Smoothstep (vectorized) is {smoothstep_vectorized_time/sigmoid_time:.2f}x slower than sigmoid") 