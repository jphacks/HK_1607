class CreateExpressionAverages < ActiveRecord::Migration[5.0]
  def change
    create_table :expression_averages do |t|
      t.float :expression_avg

      t.timestamps
    end
  end
end
